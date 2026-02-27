#!/usr/bin/env python3
"""
DLPSGame.com search helper — bypasses Cloudflare Turnstile using DrissionPage.

Called by the VB.NET app to perform searches when HTTP/WebView2 fails.
Returns JSON results to stdout.

Usage:
    python dlps_search.py --query "looney tunes" [--max 20]

Requires:
    pip install DrissionPage
"""

import argparse
import json
import re
import sys
import os

BASE_URL = "https://dlpsgame.com"

# URL segments to skip (non-game pages)
SKIP_SEGMENTS = [
    "/category/", "/tag/", "/author/", "/page/", "/wp-content/",
    "/feed/", "/wp-json/", "/wp-login/", "/wp-admin/", "/wp-includes/",
    "/comments/", "/xmlrpc", "/daily-update", "/list-all-game",
    "/list-game-ps", "/warning-about", "/dmca", "/guide-",
    "/all-guide-", "#"
]

HOST_DOMAINS = [
    "1fichier.com", "mediafire.com", "www.mediafire.com",
    "gofile.io", "akirabox.com", "vikingfile.com",
    "rootz.so", "www.rootz.so", "1cloudfile.com",
    "buzzheavier.com", "datanodes.to", "filecrypt.cc",
    "pixeldrain.com", "cyberfile.is", "uploadhaven.com",
    "fikper.com", "rapidgator.net", "nitroflare.com",
    "turbobit.net", "katfile.com", "ddownload.com"
]


def get_host_name(domain):
    d = domain.lower().replace("www.", "")
    names = {
        "1fichier.com": "1Fichier", "mediafire.com": "Mediafire",
        "gofile.io": "Gofile", "akirabox.com": "Akirabox",
        "vikingfile.com": "Vikingfile", "rootz.so": "Rootz",
        "1cloudfile.com": "1CloudFile", "buzzheavier.com": "Buzzheavier",
        "datanodes.to": "Datanodes", "filecrypt.cc": "Filecrypt",
        "pixeldrain.com": "Pixeldrain",
    }
    return names.get(d, domain)


def is_game_link(url):
    """Check if a URL points to a game page (not navigation/utility)."""
    norm = url.replace("://www.", "://").rstrip("/")
    if norm == "https://dlpsgame.com":
        return False
    for seg in SKIP_SEGMENTS:
        if seg in url.lower():
            return False
    return True


def detect_platform(title, url):
    t = title.upper()
    u = url.lower()
    if "PS5" in t or "-ps5" in u:
        return "PS5"
    if "PS4" in t or "-ps4" in u:
        return "PS4"
    if "PS3" in t or "-ps3" in u:
        return "PS3"
    if "PS2" in t or "-ps2" in u:
        return "PS2"
    if "PSX" in t or "-psx" in u:
        return "PSX"
    return ""


def extract_download_links(html):
    """Extract hosting service links from a game page."""
    links = []
    seen = set()
    for m in re.finditer(r'<a\s+[^>]*href="(https?://[^"]+)"[^>]*>', html, re.I):
        url = m.group(1)
        for domain in HOST_DOMAINS:
            if domain in url.lower() and url not in seen:
                seen.add(url)
                links.append({"host": get_host_name(domain), "url": url})
                break
    return links


def parse_download_sections(html, all_links):
    """Parse structured sections (Game, Update, DLC, Backport)."""
    sections = []
    pattern = r"(Game|Update|DLC|Backport\s*\d*\.?\w*)\s*(?:\(v([^)]+)\))?\s*:"
    matches = list(re.finditer(pattern, html, re.I))
    if not matches:
        return sections
    for i, m in enumerate(matches):
        label = m.group(1).strip()
        version = m.group(2).strip() if m.group(2) else ""
        if version:
            label = f"{label} v{version}"
        start = m.start()
        end = matches[i + 1].start() if i < len(matches) - 1 else min(start + 2000, len(html))
        segment = html[start:end]
        seg_links = extract_download_links(segment)
        if seg_links:
            sections.append({"label": label, "links": seg_links})
    return sections


def search_listings(query, max_results=20):
    """Search DLPSGame for game listings, bypassing Cloudflare."""
    try:
        from DrissionPage import ChromiumPage, ChromiumOptions
    except ImportError:
        print(json.dumps({"error": "DrissionPage not installed. Run: pip install DrissionPage"}))
        sys.exit(1)

    search_url = f"{BASE_URL}/?s={query.replace(' ', '+')}"

    co = ChromiumOptions()
    # Use Edge if available, otherwise default Chromium
    edge_path = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    if os.path.exists(edge_path):
        co.set_browser_path(edge_path)
    co.set_argument("--no-first-run")
    co.set_argument("--disable-blink-features=AutomationControlled")
    co.set_argument("--window-position=-10000,-10000")
    co.set_argument("--window-size=1280,720")
    # Suppress automation flags
    co.set_pref("credentials_enable_service", False)

    page = None
    try:
        page = ChromiumPage(co)
        page.get(search_url)

        # Wait for CF Turnstile to clear (max ~40 seconds)
        for i in range(20):
            page.wait(2)
            title = str(page.title or "")
            if ("Just a moment" not in title and
                "siamo" not in title.lower() and
                "verifica" not in title.lower() and
                i > 0):
                page.wait(2)
                break

        html = page.html
        title = str(page.title or "")

        # Check if CF is still blocking
        if "Just a moment" in title or "siamo" in title.lower():
            return {"error": "Cloudflare challenge could not be solved", "results": []}

        # Extract game links from search results
        results = []
        seen = set()

        # Parse article/entry links from search results
        a_tags = re.findall(
            r'<a\s+[^>]*href="(https?://(?:www\.)?dlpsgame\.com/[^"]+)"[^>]*>\s*(?:<img[^>]*>)?\s*([^<]*)',
            html, re.I | re.S
        )
        for href, text in a_tags:
            if not is_game_link(href):
                continue
            norm = href.replace("://www.", "://").rstrip("/")
            if norm in seen:
                continue
            seen.add(norm)

            name = text.strip()
            if not name or len(name) < 3:
                slug = norm.split("/")[-1]
                name = slug.replace("-", " ").title()

            if len(name) < 3:
                continue

            results.append({
                "title": name,
                "url": href,
                "platform": detect_platform(name, href),
                "category": "Game"
            })

            if len(results) >= max_results:
                break

        return {"results": results}

    except Exception as e:
        return {"error": str(e), "results": []}
    finally:
        if page:
            try:
                page.quit()
            except Exception:
                pass


def fetch_game_page(url):
    """Fetch a game detail page and extract download links + metadata."""
    try:
        from DrissionPage import ChromiumPage, ChromiumOptions
    except ImportError:
        return {"error": "DrissionPage not installed"}

    co = ChromiumOptions()
    edge_path = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    if os.path.exists(edge_path):
        co.set_browser_path(edge_path)
    co.set_argument("--no-first-run")
    co.set_argument("--disable-blink-features=AutomationControlled")
    co.set_argument("--window-position=-10000,-10000")
    co.set_argument("--window-size=1280,720")

    page = None
    try:
        page = ChromiumPage(co)
        page.get(url)

        for i in range(15):
            page.wait(2)
            title = str(page.title or "")
            if ("Just a moment" not in title and
                "siamo" not in title.lower() and
                i > 0):
                page.wait(2)
                break

        html = page.html

        # Extract metadata
        game_title = ""
        m = re.search(r"<h1[^>]*>([^<]+)</h1>", html, re.I)
        if m:
            game_title = m.group(1).strip()

        # PKG ID
        pkg_match = re.search(r"((?:PPSA|CUSA)\d{5})\s*[-\u2013]?\s*(USA|EUR|JPN|ASIA)?", html, re.I)
        region = pkg_match.group(2).upper() if pkg_match and pkg_match.group(2) else ""
        pkg_id = pkg_match.group(1) if pkg_match else ""

        # Firmware
        fw_match = re.search(r"(?:Works\s+on|Firmware|FW|Requires?)[^0-9]*(\d+\.\d+)", html, re.I)
        firmware = fw_match.group(1) if fw_match else ""

        # Password
        pw_match = re.search(r"Password\s*:\s*([^\r\n<]+)", html, re.I)
        password = pw_match.group(1).strip() if pw_match else ""

        # Download links
        all_links = extract_download_links(html)
        sections = parse_download_sections(html, all_links)

        return {
            "title": game_title,
            "pkg_id": pkg_id,
            "region": region,
            "firmware": firmware,
            "password": password,
            "links": all_links,
            "sections": sections
        }

    except Exception as e:
        return {"error": str(e)}
    finally:
        if page:
            try:
                page.quit()
            except Exception:
                pass


def main():
    parser = argparse.ArgumentParser(description="DLPSGame search helper")
    parser.add_argument("--query", "-q", type=str, help="Search query")
    parser.add_argument("--max", "-m", type=int, default=20, help="Max results")
    parser.add_argument("--fetch-page", type=str, help="Fetch a game detail page URL")
    args = parser.parse_args()

    if args.fetch_page:
        result = fetch_game_page(args.fetch_page)
        print(json.dumps(result, ensure_ascii=False))
    elif args.query:
        result = search_listings(args.query, args.max)
        print(json.dumps(result, ensure_ascii=False))
    else:
        parser.print_help()
        sys.exit(1)


if __name__ == "__main__":
    main()
