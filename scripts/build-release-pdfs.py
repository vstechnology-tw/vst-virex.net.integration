
from __future__ import annotations

import argparse
import hashlib
import html
import os
import re
import shutil
import subprocess
import tempfile
from pathlib import Path
from urllib.parse import urljoin, urlparse, unquote

from pypdf import PdfReader, PdfWriter
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.pdfgen import canvas


PUBLIC_ROOT = "https://vstechnology-tw.github.io/vst-virex.net.integration/"
LOCALES = (
    ("en", "", "English"),
    ("zh-Hant", "zh-Hant/", "zh-Hant"),
    ("ja", "ja/", "ja"),
    ("ko", "ko/", "ko"),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build link-preserving PDF documentation assets.")
    parser.add_argument("--site-path", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--mermaid-cli", type=Path)
    return parser.parse_args()


def resolve_nav(repo_root: Path) -> list[str]:
    text = (repo_root / "mkdocs.yml").read_text(encoding="utf-8")
    nav_start = text.find("\nnav:")
    if nav_start < 0:
        raise RuntimeError("mkdocs.yml does not contain a nav section.")

    paths: list[str] = []
    for line in text[nav_start:].splitlines():
        match = re.search(r":\s*([A-Za-z0-9_./-]+\.md)\s*$", line)
        if match and match.group(1) not in paths:
            paths.append(match.group(1))

    if len(paths) < 10:
        raise RuntimeError(f"Only {len(paths)} documentation pages were found in mkdocs.yml.")
    return paths


def public_page_url(prefix: str, markdown_path: str) -> str:
    return urljoin(PUBLIC_ROOT, prefix + markdown_path.removesuffix(".md") + ".html")


def local_page_path(site_path: Path, prefix: str, markdown_path: str) -> Path:
    return site_path / prefix / (markdown_path.removesuffix(".md") + ".html")


def prefix_fragment_ids(fragment: str, section_prefix: str) -> str:
    id_pattern = re.compile(r"\bid=([\"'])([^\"']+)\1", re.IGNORECASE)
    svg_fragments: list[str] = []

    def protect_svg(match: re.Match[str]) -> str:
        svg = match.group(0)
        svg_id_pattern = re.compile(r"\bid=([\"'])([^\"']+)\1", re.IGNORECASE)
        id_map = {
            match.group(2): f"{section_prefix}--mermaid-{len(svg_fragments)}--{match.group(2)}"
            for match in svg_id_pattern.finditer(svg)
        }
        for old_id, new_id in id_map.items():
            svg = re.sub(
                rf"(\bid=([\"'])){re.escape(old_id)}([\"'])",
                rf"\g<1>{new_id}\g<3>",
                svg,
                flags=re.IGNORECASE,
            )
            svg = re.sub(rf"(?<![\w-])#{re.escape(old_id)}\b", f"#{new_id}", svg)
            svg = svg.replace(f"url(#{old_id})", f"url(#{new_id})")
        svg_fragments.append(svg)
        return f"__PDF_SVG_{len(svg_fragments) - 1}__"

    protected = re.sub(r"<svg\b.*?</svg>", protect_svg, fragment, flags=re.IGNORECASE | re.DOTALL)
    prefixed = id_pattern.sub(
        lambda match: f'id={match.group(1)}{section_prefix}--{match.group(2)}{match.group(1)}',
        protected,
    )
    return re.sub(
        r"__PDF_SVG_(\d+)__",
        lambda match: svg_fragments[int(match.group(1))],
        prefixed,
    )


DIV_OPEN_PATTERN = re.compile(r"<div\b[^>]*>", re.IGNORECASE)
DIV_TAG_PATTERN = re.compile(r"</?div\b[^>]*>", re.IGNORECASE)
TABBED_SET_OPEN_PATTERN = re.compile(
    r'''<div\b[^>]*\bclass=["'][^"']*\btabbed-set\b[^"']*["'][^>]*>''',
    re.IGNORECASE,
)
MERMAID_BLOCK_PATTERN = re.compile(
    r'''<pre\b[^>]*\bclass=["'][^"']*\bmermaid\b[^"']*["'][^>]*>\s*<code\b[^>]*>(.*?)</code>\s*</pre>''',
    re.IGNORECASE | re.DOTALL,
)


def matching_div_bounds(markup: str, opening_start: int) -> tuple[int, int]:
    depth = 0
    for tag in DIV_TAG_PATTERN.finditer(markup, opening_start):
        if tag.group(0).startswith("</"):
            depth -= 1
            if depth == 0:
                return tag.start(), tag.end()
        else:
            depth += 1
    raise RuntimeError("Could not find the closing div for a tabbed example.")


def div_inner_bounds(markup: str, opening_start: int) -> tuple[int, int, int]:
    opening = DIV_OPEN_PATTERN.match(markup, opening_start)
    if not opening:
        raise RuntimeError("Expected a div opening tag.")
    closing_start, closing_end = matching_div_bounds(markup, opening_start)
    return opening.end(), closing_start, closing_end


def find_div_opening_by_class(markup: str, class_name: str, start: int = 0) -> re.Match[str] | None:
    pattern = re.compile(
        rf'''<div\b[^>]*\bclass=["'][^"']*\b{re.escape(class_name)}\b[^"']*["'][^>]*>''',
        re.IGNORECASE,
    )
    return pattern.search(markup, start)


def direct_div_contents(markup: str, container_start: int, container_end: int, class_name: str) -> list[str]:
    opening_pattern = re.compile(
        rf'''<div\b[^>]*\bclass=["'][^"']*\b{re.escape(class_name)}\b[^"']*["'][^>]*>''',
        re.IGNORECASE,
    )
    contents: list[str] = []
    next_start = container_start
    for opening in opening_pattern.finditer(markup, container_start, container_end):
        if opening.start() < next_start:
            continue
        closing_start, closing_end = matching_div_bounds(markup, opening.start())
        if closing_end > container_end:
            break
        contents.append(markup[opening.end():closing_start])
        next_start = closing_end
    return contents


def render_tabbed_set(markup: str) -> str:
    labels_opening = find_div_opening_by_class(markup, "tabbed-labels")
    content_opening = find_div_opening_by_class(markup, "tabbed-content")
    if not labels_opening or not content_opening:
        return markup

    labels_start, labels_end, _ = div_inner_bounds(markup, labels_opening.start())
    labels = [
        html.unescape(re.sub(r"<[^>]+>", "", match.group(1))).strip()
        for match in re.finditer(
            r"<label\b[^>]*>(.*?)</label>",
            markup[labels_start:labels_end],
            re.IGNORECASE | re.DOTALL,
        )
    ]
    content_start, content_end, _ = div_inner_bounds(markup, content_opening.start())
    blocks = direct_div_contents(markup, content_start, content_end, "tabbed-block")
    if not blocks:
        return markup

    rendered_blocks = []
    for index, block in enumerate(blocks):
        title = labels[index] if index < len(labels) else f"Example {index + 1}"
        rendered_blocks.append(
            f'<section class="pdf-tab-block"><h4 class="pdf-tab-title">{html.escape(title)}</h4>'
            f'{expand_tabbed_sets(block)}</section>'
        )
    return f'<div class="pdf-tabbed-set">{"".join(rendered_blocks)}</div>'


def expand_tabbed_sets(fragment: str) -> str:
    pieces: list[str] = []
    cursor = 0
    search_start = 0
    while match := TABBED_SET_OPEN_PATTERN.search(fragment, search_start):
        _, _, closing_end = div_inner_bounds(fragment, match.start())
        pieces.append(fragment[cursor:match.start()])
        pieces.append(render_tabbed_set(fragment[match.start():closing_end]))
        cursor = closing_end
        search_start = closing_end
    pieces.append(fragment[cursor:])
    return "".join(pieces)


def find_mermaid_cli(explicit_path: Path | None = None) -> Path:
    candidates = []
    if explicit_path:
        candidates.append(explicit_path)
    discovered = shutil.which("mmdc")
    if discovered:
        candidates.append(Path(discovered))
    app_data = os.environ.get("APPDATA")
    if app_data:
        candidates.append(Path(app_data) / "npm" / "mmdc.cmd")

    for candidate in candidates:
        if candidate.exists():
            return candidate.resolve()
    raise RuntimeError(
        "Mermaid CLI (mmdc) was not found. Install @mermaid-js/mermaid-cli or pass --mermaid-cli."
    )


def render_mermaid_blocks(fragment: str, mermaid_cli: Path, render_dir: Path) -> str:
    def replace(match: re.Match[str]) -> str:
        source = html.unescape(re.sub(r"<[^>]+>", "", match.group(1)))
        digest = hashlib.sha256(source.encode("utf-8")).hexdigest()[:16]
        input_path = render_dir / f"mermaid-{digest}.mmd"
        output_path = render_dir / f"mermaid-{digest}.svg"
        if not output_path.exists():
            input_path.write_text(source, encoding="utf-8")
            command = [str(mermaid_cli), "-i", str(input_path), "-o", str(output_path), "-b", "transparent"]
            if mermaid_cli.suffix.lower() in {".bat", ".cmd"}:
                command = ["cmd.exe", "/d", "/s", "/c", *command]
            elif mermaid_cli.suffix.lower() == ".ps1":
                command = ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", *command]
            result = subprocess.run(command, capture_output=True, text=True)
            if result.returncode != 0:
                detail = (result.stderr or result.stdout).strip()[-2000:]
                raise RuntimeError(f"Mermaid rendering failed for {input_path.name}: {detail}")
        svg = output_path.read_text(encoding="utf-8")
        if "<svg" not in svg:
            raise RuntimeError(f"Mermaid renderer produced invalid SVG: {output_path}")
        return f'<figure class="pdf-mermaid">{svg}</figure>'

    rendered = MERMAID_BLOCK_PATTERN.sub(replace, fragment)
    if MERMAID_BLOCK_PATTERN.search(rendered):
        raise RuntimeError("A Mermaid source block remained after SVG rendering.")
    return rendered


def annotate_external_links(fragment: str) -> str:
    anchor_pattern = re.compile(
        r"(<a\b[^>]*\bhref=([\"'])(https?://[^\"']+)\2[^>]*>)(.*?)</a>",
        re.IGNORECASE | re.DOTALL,
    )

    def replace(match: re.Match[str]) -> str:
        if "external-link-note" in match.group(4):
            return match.group(0)
        return f'{match.group(1)}{match.group(4)}<span class="external-link-note"> [external link]</span></a>'

    return anchor_pattern.sub(replace, fragment)


def rewrite_attributes(
    fragment: str,
    source_page: Path,
    public_url: str,
    internal_targets: dict[str, int],
) -> str:
    attribute_pattern = re.compile(r"\b(href|src)=([\"'])(.*?)\2", re.IGNORECASE)
    public_host = urlparse(PUBLIC_ROOT).netloc

    def replace(match: re.Match[str]) -> str:
        name, quote, value = match.group(1), match.group(2), match.group(3)
        if value.startswith(("data:", "mailto:", "javascript:")):
            return match.group(0)

        if name.lower() == "href":
            absolute = urljoin(public_url, value)
            parsed = urlparse(absolute)
            target_index = internal_targets.get(parsed.path) if parsed.netloc == public_host else None
            if target_index is not None:
                target = f"#doc-{target_index}"
                if parsed.fragment:
                    target += f"--{parsed.fragment}"
                return f"{name}={quote}{target}{quote}"
            return f"{name}={quote}{absolute}{quote}"

        parsed = urlparse(value)
        if parsed.scheme in {"http", "https", "data"}:
            return match.group(0)

        image_path = (source_page.parent / unquote(parsed.path)).resolve()
        if image_path.exists():
            absolute = image_path.as_uri()
            if parsed.query:
                absolute += "?" + parsed.query
            if parsed.fragment:
                absolute += "#" + parsed.fragment
            return f"{name}={quote}{absolute}{quote}"

        return match.group(0)

    return annotate_external_links(attribute_pattern.sub(replace, fragment))


def extract_main(
    source_page: Path,
    public_url: str,
    internal_targets: dict[str, int],
    document_index: int,
    mermaid_cli: Path | None = None,
    render_dir: Path | None = None,
) -> tuple[str, str]:
    source = source_page.read_text(encoding="utf-8")
    match = re.search(r"<main\b[^>]*>(.*?)</main>", source, re.IGNORECASE | re.DOTALL)
    if not match:
        raise RuntimeError(f"Could not find main content in {source_page}")

    fragment = match.group(1)
    fragment = re.sub(r"<script\b.*?</script>", "", fragment, flags=re.IGNORECASE | re.DOTALL)
    fragment = re.sub(r"<button\b.*?</button>", "", fragment, flags=re.IGNORECASE | re.DOTALL)
    fragment = re.sub(r"<aside\b.*?</aside>", "", fragment, flags=re.IGNORECASE | re.DOTALL)
    fragment = expand_tabbed_sets(fragment)
    if MERMAID_BLOCK_PATTERN.search(fragment):
        if mermaid_cli is None or render_dir is None:
            raise RuntimeError(f"Mermaid rendering is required for {source_page}")
        fragment = render_mermaid_blocks(fragment, mermaid_cli, render_dir)
    section_prefix = f"doc-{document_index}"
    fragment = prefix_fragment_ids(fragment, section_prefix)
    fragment = rewrite_attributes(fragment, source_page, public_url, internal_targets)

    heading = re.search(r"<h1\b[^>]*>(.*?)</h1>", fragment, re.IGNORECASE | re.DOTALL)
    title = re.sub(r"<[^>]+>", "", heading.group(1) if heading else source_page.stem)
    title = html.unescape(re.sub(r"\s+", " ", title)).strip()
    return title, fragment


def find_edge() -> Path:
    candidates = (
        Path(os.environ.get("ProgramFiles(x86)", "")) / "Microsoft/Edge/Application/msedge.exe",
        Path(os.environ.get("ProgramFiles", "")) / "Microsoft/Edge/Application/msedge.exe",
    )
    for candidate in candidates:
        if candidate.exists():
            return candidate
    raise RuntimeError("Microsoft Edge was not found on the GitHub Actions runner.")


def write_pdf_numbers(raw_pdf: Path, final_pdf: Path, label: str, version: str) -> None:
    reader = PdfReader(str(raw_pdf))
    page_count = len(reader.pages)

    with tempfile.TemporaryDirectory(prefix="virex-pdf-overlay-") as temp_dir:
        overlay_path = Path(temp_dir) / "page-numbers.pdf"
        overlay = canvas.Canvas(str(overlay_path), pagesize=A4)
        overlay.setFillColorRGB(1, 1, 1)
        overlay.rect(0, 0, A4[0], 15 * mm, fill=1, stroke=0)
        overlay.rect(0, A4[1] - 15 * mm, A4[0], 15 * mm, fill=1, stroke=0)
        overlay.setFillColorRGB(0.40, 0.45, 0.50)
        overlay.setFont("Helvetica", 8)

        for page_number in range(1, page_count + 1):
            footer = f"Virex.NET Integration Kit - {label} - v{version} - {page_number} / {page_count}"
            overlay.drawCentredString(A4[0] / 2, 9 * mm, footer)
            overlay.showPage()
        overlay.save()

        overlay_reader = PdfReader(str(overlay_path))
        writer = PdfWriter()
        writer.add_metadata({
            "/Title": f"Virex.NET Integration Kit v{version} - {label}",
            "/Subject": "Public Virex.NET integration documentation",
            "/Author": "Virex.NET Integration Kit",
        })

        for index, page in enumerate(reader.pages):
            page.merge_page(overlay_reader.pages[index])
            writer.add_page(page)

        with final_pdf.open("wb") as stream:
            writer.write(stream)


def build_locale(
    site_path: Path,
    output_dir: Path,
    version: str,
    page_paths: list[str],
    locale_id: str,
    prefix: str,
    label: str,
    edge_path: Path,
    mermaid_cli: Path,
) -> Path:
    internal_targets = {
        urlparse(public_page_url(prefix, markdown_path)).path: index
        for index, markdown_path in enumerate(page_paths)
    }
    documents: list[tuple[str, str, str]] = []
    with tempfile.TemporaryDirectory(prefix=f"virex-mermaid-{locale_id}-") as mermaid_dir:
        render_dir = Path(mermaid_dir)
        for index, markdown_path in enumerate(page_paths):
            source_page = local_page_path(site_path, prefix, markdown_path)
            if not source_page.exists():
                raise RuntimeError(f"Missing generated page: {source_page}")
            public_url = public_page_url(prefix, markdown_path)
            title, fragment = extract_main(
                source_page,
                public_url,
                internal_targets,
                index,
                mermaid_cli,
                render_dir,
            )
            documents.append((title, public_url, fragment))

    toc = "\n".join(
        f'<li><a href="#doc-{index}">{html.escape(title)}</a></li>'
        for index, (title, _, _) in enumerate(documents)
    )
    body = "\n".join(
        f'<section class="doc" id="doc-{index}">'
        f'<div class="source"><a href="{url}">View this page online <span class="external-link-note">[external link]</span></a></div>{fragment}</section>'
        for index, (_, url, fragment) in enumerate(documents)
    )

    css = """
@page { size: A4; margin: 15mm 14mm 17mm 14mm; }
* { box-sizing: border-box; }
html, body { margin: 0; padding: 0; background: #fff; color: #1f2933; font-family: "Segoe UI", "Noto Sans CJK TC", "Yu Gothic", "Malgun Gothic", Arial, sans-serif; font-size: 10.5pt; line-height: 1.52; }
.cover { min-height: 245mm; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; break-after: page; page-break-after: always; padding: 20mm; }
.cover .rule { width: 42mm; height: 2px; background: #2688aa; margin: 10mm auto; }
.cover h1 { color: #1f6078; font-size: 30pt; line-height: 1.15; margin: 0; }
.cover h2 { color: #53636f; font-size: 16pt; font-weight: 400; margin: 7mm 0 0; }
.cover .meta { color: #65727e; font-size: 9pt; margin-top: 14mm; }
.toc { break-after: page; page-break-after: always; }
.toc h1 { color: #1f6078; border-bottom: 2px solid #2688aa; padding-bottom: 3mm; }
.toc ol { margin: 6mm 0; padding-left: 8mm; }
.toc li { margin: 1.7mm 0; }
.toc a, a { color: #146b8a; text-decoration: underline; }
.doc { break-before: page; page-break-before: always; }
.doc:first-of-type { break-before: auto; page-break-before: auto; }
.doc h1, .doc h2, .doc h3, .doc h4 { color: #1f6078; break-after: avoid; page-break-after: avoid; }
.doc h1 { font-size: 23pt; line-height: 1.2; border-bottom: 2px solid #2688aa; padding-bottom: 3mm; margin: 0 0 7mm; }
.doc h2 { font-size: 17pt; border-bottom: 1px solid #c5d5dc; padding-bottom: 1.5mm; margin-top: 9mm; }
.doc h3 { font-size: 13.5pt; margin-top: 7mm; }
.doc h4 { font-size: 11.5pt; margin-top: 5mm; }
.pdf-tabbed-set { margin: 5mm 0 7mm; }
.pdf-tab-block { border: 1px solid #d6dfe3; border-radius: 3px; background: #fbfcfd; margin: 4mm 0 6mm; padding: 3mm; break-inside: avoid; page-break-inside: avoid; }
.doc .pdf-tab-title { color: #204e61; background: #e9f1f4; border-bottom: 1px solid #c5d5dc; font-size: 11pt; line-height: 1.25; margin: -3mm -3mm 3mm; padding: 2mm 3mm; }
.pdf-tab-block .highlight { margin: 0; }
.pdf-mermaid { margin: 5mm 0 7mm; padding: 3mm; border: 1px solid #d6dfe3; border-radius: 3px; background: #fff; break-inside: avoid; page-break-inside: avoid; text-align: center; }
.pdf-mermaid svg { width: 100%; max-height: 230mm; margin: 0 auto; }
p, ul, ol, blockquote, pre, table, figure { margin-top: 3.5mm; margin-bottom: 3.5mm; }
ul, ol { padding-left: 7mm; }
blockquote { border-left: 3px solid #2688aa; background: #f3f7f9; padding: 2mm 4mm; margin-left: 0; }
code { color: #25485a; background: #edf2f4; padding: 0.1em 0.3em; border-radius: 2px; overflow-wrap: anywhere; }
pre { background: #f4f6f7; border: 1px solid #d6dfe3; border-radius: 3px; padding: 3.5mm; white-space: pre-wrap; overflow-wrap: anywhere; font-family: "Cascadia Mono", "Consolas", monospace; font-size: 8.2pt; line-height: 1.42; break-inside: avoid; page-break-inside: avoid; }
pre code { background: transparent; padding: 0; color: #263238; }
table { width: 100%; border-collapse: collapse; font-size: 8.9pt; break-inside: auto; }
thead { display: table-header-group; }
th, td { border: 1px solid #cbd6db; padding: 1.8mm 2mm; vertical-align: top; overflow-wrap: anywhere; }
th { background: #e9f1f4; color: #204e61; font-weight: 600; }
tr { break-inside: avoid; page-break-inside: avoid; }
img, svg { max-width: 100%; height: auto; display: block; margin: 4mm auto; break-inside: avoid; page-break-inside: avoid; }
.source { text-align: right; font-size: 8pt; margin-bottom: 3mm; }
.external-link-note { color: #8b4a00; font-size: 8pt; font-weight: 600; white-space: nowrap; }
.headerlink, .md-clipboard, button, nav, aside, .md-sidebar, .md-header, .md-tabs, .md-footer { display: none !important; }
"""

    document = f"""<!doctype html>
<html lang="{locale_id}">
<head>
<meta charset="utf-8">
<title>Virex.NET Integration Kit v{version} - {html.escape(label)}</title>
<style>{css}</style>
</head>
<body>
<section class="cover">
<h1>Virex.NET Integration Kit</h1>
<div class="rule"></div>
<h2>{html.escape(label)} Documentation</h2>
<div class="meta">Public integration guide - Generated by the release workflow<br>v{html.escape(version)}</div>
</section>
<section class="toc"><h1>Contents</h1><ol>{toc}</ol></section>
{body}
</body>
</html>
"""

    with tempfile.TemporaryDirectory(prefix=f"virex-doc-{locale_id}-") as temp_dir:
        temp = Path(temp_dir)
        combined = temp / "documentation.html"
        raw_pdf = temp / "raw.pdf"
        combined.write_text(document, encoding="utf-8")

        subprocess.run(
            [
                str(edge_path),
                "--headless",
                "--disable-gpu",
                "--allow-file-access-from-files",
                "--no-pdf-header-footer",
                f"--print-to-pdf={raw_pdf}",
                combined.as_uri(),
            ],
            check=True,
            capture_output=True,
            text=True,
        )

        final_path = output_dir / f"Virex.NET.Documentation-v{version}-{locale_id}.pdf"
        write_pdf_numbers(raw_pdf, final_path, label, version)
        return final_path


def main() -> None:
    args = parse_args()
    site_path = args.site_path.resolve()
    output_dir = args.output.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    page_paths = resolve_nav(args.repo_root.resolve())
    edge_path = find_edge()
    mermaid_cli = find_mermaid_cli(args.mermaid_cli)

    for locale_id, prefix, label in LOCALES:
        result = build_locale(
            site_path,
            output_dir,
            args.version,
            page_paths,
            locale_id,
            prefix,
            label,
            edge_path,
            mermaid_cli,
        )
        print(result)


if __name__ == "__main__":
    main()
