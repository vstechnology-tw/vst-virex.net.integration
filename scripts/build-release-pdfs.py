
from __future__ import annotations

import argparse
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
    return id_pattern.sub(
        lambda match: f'id={match.group(1)}{section_prefix}--{match.group(2)}{match.group(1)}',
        fragment,
    )


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
) -> tuple[str, str]:
    source = source_page.read_text(encoding="utf-8")
    match = re.search(r"<main\b[^>]*>(.*?)</main>", source, re.IGNORECASE | re.DOTALL)
    if not match:
        raise RuntimeError(f"Could not find main content in {source_page}")

    fragment = match.group(1)
    fragment = re.sub(r"<script\b.*?</script>", "", fragment, flags=re.IGNORECASE | re.DOTALL)
    fragment = re.sub(r"<button\b.*?</button>", "", fragment, flags=re.IGNORECASE | re.DOTALL)
    fragment = re.sub(r"<aside\b.*?</aside>", "", fragment, flags=re.IGNORECASE | re.DOTALL)
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
) -> Path:
    internal_targets = {
        urlparse(public_page_url(prefix, markdown_path)).path: index
        for index, markdown_path in enumerate(page_paths)
    }
    documents: list[tuple[str, str, str]] = []
    for index, markdown_path in enumerate(page_paths):
        source_page = local_page_path(site_path, prefix, markdown_path)
        if not source_page.exists():
            raise RuntimeError(f"Missing generated page: {source_page}")
        public_url = public_page_url(prefix, markdown_path)
        title, fragment = extract_main(source_page, public_url, internal_targets, index)
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
        )
        print(result)


if __name__ == "__main__":
    main()
