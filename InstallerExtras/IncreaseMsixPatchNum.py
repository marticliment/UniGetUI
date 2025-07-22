import re
from pathlib import Path
import sys

def main():
    manifest_path = Path(__file__).parent / 'AppxManifest.xml'
    if not manifest_path.exists():
        print(f"Cannot find {manifest_path}")
        sys.exit(1)

    content = manifest_path.read_text(encoding='utf-8')

    def bump_version(match):
        major, minor, build, rev = match.groups()
        new_rev = int(rev) + 1
        old_version = f"{major}.{minor}.{build}.{rev}"
        new_version = f"{major}.{minor}.{build}.{new_rev}"
        print(f"Updated patch version: {old_version} → {new_version}")
        return f'Version="{new_version}"'

    new_content, count = re.subn(
        r'Version="(\d+)\.(\d+)\.(\d+)\.(\d+)"',
        bump_version,
        content,
        count=1
    )

    if count == 0:
        print("No matching Version attribute found")
        sys.exit(1)

    manifest_path.write_text(new_content, encoding='utf-8')

if __name__ == '__main__':
    main()
