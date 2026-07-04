#!/usr/bin/env python3
import argparse
import json
from datetime import datetime, timezone
from pathlib import Path


PLUGIN = {
    "guid": "29d31092-3377-48c6-a161-ef432d6a61e4",
    "name": "Playback Trace",
    "description": "Records Jellyfin playback progress and exports watched timeline segments, seeks, pauses, resumes, and raw playback events as HTML tables.",
    "overview": "Detailed server-side playback timeline tracing",
    "owner": "Kelvin00008",
    "category": "Administration",
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--version", required=True)
    parser.add_argument("--target-abi", default="10.10.3.0")
    parser.add_argument("--source-url", required=True)
    parser.add_argument("--checksum", required=True)
    parser.add_argument("--output", default="manifest.json")
    args = parser.parse_args()

    manifest = [
        {
            **PLUGIN,
            "versions": [
                {
                    "version": args.version,
                    "changelog": "Initial build with watched segment tracking and HTML export.",
                    "targetAbi": args.target_abi,
                    "sourceUrl": args.source_url,
                    "checksum": args.checksum,
                    "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
                }
            ],
        }
    ]

    Path(args.output).write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
