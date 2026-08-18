from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "build-release-pdfs.py"


def load_pdf_builder():
    spec = importlib.util.spec_from_file_location("build_release_pdfs", SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load {SCRIPT}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class ReleasePdfRenderingTests(unittest.TestCase):
    def test_mermaid_source_is_replaced_with_styled_svg(self) -> None:
        builder = load_pdf_builder()
        mermaid_cli = builder.find_mermaid_cli()
        source_page = ROOT / "site" / "integration-model.html"

        with tempfile.TemporaryDirectory(prefix="virex-mermaid-test-") as temp_dir:
            _, fragment = builder.extract_main(
                source_page,
                "https://vstechnology-tw.github.io/vst-virex.net.integration/integration-model.html",
                {},
                0,
                mermaid_cli,
                Path(temp_dir),
            )

        self.assertNotIn("flowchart LR", fragment)
        self.assertIn('<figure class="pdf-mermaid">', fragment)
        self.assertIn('class="flowchart"', fragment)
        self.assertNotIn("#my-svg", fragment)


if __name__ == "__main__":
    unittest.main()
