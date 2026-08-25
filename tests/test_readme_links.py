import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
README = ROOT / "README.md"
PACKAGE_LINK_PATTERN = re.compile(
    r"\[!\[Virex\.NET\.(?:Contracts|Client)\]\([^)]*\)\]"
    r"\((https://www\.nuget\.org/packages/Virex\.NET\.(?:Contracts|Client)[^)]*)\)"
)
EXPECTED_PACKAGE_LINKS = [
    "https://www.nuget.org/packages/Virex.NET.Contracts",
    "https://www.nuget.org/packages/Virex.NET.Client",
]


class ReadmePackageLinkTests(unittest.TestCase):
    def test_nuget_badges_use_version_independent_package_links(self) -> None:
        readme = README.read_text(encoding="utf-8")

        self.assertEqual(EXPECTED_PACKAGE_LINKS, PACKAGE_LINK_PATTERN.findall(readme))


if __name__ == "__main__":
    unittest.main()
