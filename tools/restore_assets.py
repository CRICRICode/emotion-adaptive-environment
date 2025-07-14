"""Restore the original Unity assets from the checked archives (standard library only)."""
import argparse
import gzip
import hashlib
import io
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BLOCK = 4 * 1024 * 1024


def safe_path(relative):
    path = (ROOT / relative).resolve()
    if not path.is_relative_to(ROOT):
        raise ValueError(f'Path outside repository: {relative}')
    return path


def file_hash(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        while block := stream.read(BLOCK):
            digest.update(block)
    return digest.hexdigest()


class PartReader(io.RawIOBase):
    def __init__(self, paths):
        self.paths = iter(paths)
        self.stream = None

    def readable(self):
        return True

    def readinto(self, buffer):
        while True:
            if self.stream is None:
                try:
                    self.stream = next(self.paths).open('rb')
                except StopIteration:
                    return 0
            size = self.stream.readinto(buffer)
            if size:
                return size
            self.stream.close()
            self.stream = None

    def close(self):
        if self.stream is not None:
            self.stream.close()
        super().close()


def restore(entry, check=False):
    target = safe_path(entry['path'])
    if not target.is_relative_to(ROOT / 'Assets'):
        raise ValueError('Only Unity assets may be restored')
    if not check and target.exists():
        if target.stat().st_size == entry['size'] and file_hash(target) == entry['sha256']:
            print(f'Already restored: {entry["path"]}', flush=True)
            return
        raise ValueError(f'Existing file differs from archive: {entry["path"]}. Move it aside before restoring.')

    parts = []
    for part in entry['parts']:
        path = safe_path(part['path'])
        if not path.is_relative_to(ROOT / 'large-assets'):
            raise ValueError('Archive part must be inside large-assets')
        if not path.is_file() or path.stat().st_size != part['size']:
            raise ValueError(f'Missing or incomplete archive: {part["path"]}. Run git lfs pull.')
        if file_hash(path) != part['sha256']:
            raise ValueError(f'Archive checksum mismatch: {part["path"]}')
        parts.append(path)

    temporary = target.with_name(target.name + '.restoring')
    if not check and temporary.exists():
        raise ValueError(f'Interrupted restore exists: {temporary.relative_to(ROOT)}. Move it aside before retrying.')
    output = None
    created = False
    digest = hashlib.sha256()
    size = 0
    try:
        if not check:
            target.parent.mkdir(parents=True, exist_ok=True)
            output = temporary.open('xb')
            created = True
        with io.BufferedReader(PartReader(parts), buffer_size=BLOCK) as stream, gzip.GzipFile(fileobj=stream, mode='rb') as original:
            while block := original.read(BLOCK):
                size += len(block)
                if size > entry['size']:
                    raise ValueError(f'Unexpected expanded size: {entry["path"]}')
                digest.update(block)
                if output is not None:
                    output.write(block)
        if size != entry['size'] or digest.hexdigest() != entry['sha256']:
            raise ValueError(f'Original checksum mismatch: {entry["path"]}')
        if output is not None:
            output.close()
            output = None
            if target.exists():
                raise ValueError(f'Target appeared while restoring: {entry["path"]}')
            temporary.rename(target)
        print(f'{"Verified" if check else "Restored"}: {entry["path"]}', flush=True)
    finally:
        if output is not None:
            output.close()
        if created and temporary.exists():
            temporary.unlink()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Verify every archive and original checksum without writing assets')
    args = parser.parse_args()
    manifest = json.loads((ROOT / 'large-assets' / 'manifest.json').read_text(encoding='utf-8'))
    if manifest.get('format') != 1 or manifest.get('compression') != 'gzip':
        raise ValueError('Unsupported archive format')
    required = sum(entry['size'] for entry in manifest['files'])
    print(f'{len(manifest["files"])} assets, {required / 1024**3:.2f} GiB expanded. Close Unity before restoring.', flush=True)
    for entry in manifest['files']:
        restore(entry, check=args.check)
    print('All asset checksums match.', flush=True)


if __name__ == '__main__':
    main()
