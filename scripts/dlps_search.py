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

# Persistent user data dir for browser — retains CF cookies between calls
_USER_DATA_DIR = os.path.join(os.environ.get("TEMP", "/tmp"), "dlps_browser_profile")


def _create_browser_options():
    """Create ChromiumOptions with anti-detection and persistent profile."""
    from DrissionPage import ChromiumOptions
    co = ChromiumOptions()
    edge_path = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    if os.path.exists(edge_path):
        co.set_browser_path(edge_path)
    co.set_argument("--no-first-run")
    co.set_argument("--disable-blink-features=AutomationControlled")
    co.set_argument("--window-position=-10000,-10000")
    co.set_argument("--window-size=1280,720")
    co.set_user_data_path(_USER_DATA_DIR)
    co.set_pref("credentials_enable_service", False)
    return co


def _wait_for_cf(page, max_polls=30):
    """Wait for Cloudflare Turnstile challenge to clear. Returns True if cleared."""
    for i in range(max_polls):
        page.wait(2)
        title = str(page.title or "")
        if ("Just a moment" not in title and
            "siamo" not in title.lower() and
            "verifica" not in title.lower() and
            i > 0):
            page.wait(3)
            return True
    return False


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


def decode_clksh_url(clksh_url):
    """Decode a clk.sh redirect URL to get the real hosting URL.

    DLPSGame wraps download links via clk.sh with base64-encoded target URLs:
    https://clk.sh/full?api=...&url=BASE64_ENCODED_URL&type=2
    """
    # Handle HTML entity encoding (&amp; → &)
    clean = clksh_url.replace("&amp;", "&")
    m = re.search(r'[?&]url=([A-Za-z0-9+/=]+)', clean)
    if m:
        try:
            import base64
            decoded = base64.b64decode(m.group(1)).decode("utf-8", errors="replace")
            if decoded.startswith("http"):
                return decoded
        except Exception:
            pass
    return None


def extract_download_links_from_page(page):
    """Extract hosting service links from a rendered page using DrissionPage API.

    Uses the element API to get actual href values (with HTML entities resolved),
    then decodes clk.sh base64 wrappers to get real hosting URLs.
    Also works with direct hosting links (older page format).
    """
    links = []
    seen = set()

    try:
        elements = page.eles("tag:a")
    except Exception:
        elements = []

    for el in elements:
        try:
            href = el.attr("href") or ""
        except Exception:
            continue

        real_url = None

        # clk.sh wrapped link — decode base64 target
        if "clk.sh" in href:
            real_url = decode_clksh_url(href)
        elif href.startswith("http"):
            real_url = href

        if not real_url:
            continue

        for domain in HOST_DOMAINS:
            if domain in real_url.lower() and real_url not in seen:
                seen.add(real_url)
                # Use link text as display name if available, else derive from domain
                text = ""
                try:
                    text = (el.text or "").strip()
                except Exception:
                    pass
                host_name = text if text and len(text) < 30 else get_host_name(domain)
                links.append({"host": host_name, "url": real_url})
                break

    return links


def extract_download_links(html):
    """Extract hosting service links from raw HTML string.

    Handles both clk.sh redirect wrappers (with &amp; entities)
    and direct hosting URLs.
    """
    links = []
    seen = set()

    # Handle clk.sh links (with &amp; HTML entities)
    for m in re.finditer(r'href=["\']?(https?://clk\.sh/[^"\'>\s]+)', html, re.I):
        raw = m.group(1)
        real_url = decode_clksh_url(raw)
        if not real_url:
            continue
        for domain in HOST_DOMAINS:
            if domain in real_url.lower() and real_url not in seen:
                seen.add(real_url)
                links.append({"host": get_host_name(domain), "url": real_url})
                break

    # Direct hosting links
    for m in re.finditer(r'<a\s+[^>]*href="(https?://[^"]+)"[^>]*>', html, re.I):
        url = m.group(1)
        if "clk.sh" in url:
            continue
        for domain in HOST_DOMAINS:
            if domain in url.lower() and url not in seen:
                seen.add(url)
                links.append({"host": get_host_name(domain), "url": url})
                break

    return links


def parse_download_sections(html, all_links):
    """Parse structured sections (Game, Update, DLC, Backport).

    Uses the flat all_links list and assigns links to sections based on
    their position in the HTML relative to section headers.
    """
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
        end = matches[i + 1].start() if i < len(matches) - 1 else min(start + 3000, len(html))
        segment = html[start:end]

        # Find links in this HTML segment (handles &amp; entities)
        seg_links = extract_download_links(segment)
        if seg_links:
            sections.append({"label": label, "links": seg_links})

    return sections


def search_listings(query, max_results=20):
    """Search DLPSGame for game listings, bypassing Cloudflare."""
    try:
        from DrissionPage import ChromiumPage
    except ImportError:
        print(json.dumps({"error": "DrissionPage not installed. Run: pip install DrissionPage"}))
        sys.exit(1)

    search_url = f"{BASE_URL}/?s={query.replace(' ', '+')}"
    co = _create_browser_options()

    page = None
    try:
        page = ChromiumPage(co)
        page.get(search_url)

        if not _wait_for_cf(page):
            return {"error": "Cloudflare challenge could not be solved", "results": []}

        html = page.html
        title = str(page.title or "")

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
        from DrissionPage import ChromiumPage
    except ImportError:
        return {"error": "DrissionPage not installed"}

    co = _create_browser_options()

    page = None
    try:
        page = ChromiumPage(co)
        page.get(url)

        if not _wait_for_cf(page):
            return {"error": "Cloudflare challenge timeout", "links": [], "sections": []}

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

        # Download links — use page element API to resolve clk.sh wrappers
        all_links = extract_download_links_from_page(page)
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


def full_search(query, max_results=20, fetch_details=5):
    """Search DLPSGame and fetch download links for top results in one browser session.

    Solves CF once, then navigates to each game page to extract hosting links.
    Returns results with download links ready for the download manager.
    """
    try:
        from DrissionPage import ChromiumPage
    except ImportError:
        return {"error": "DrissionPage not installed. Run: pip install DrissionPage", "results": []}

    co = _create_browser_options()
    page = None

    try:
        page = ChromiumPage(co)

        # Step 1: Search
        search_url = f"{BASE_URL}/?s={query.replace(' ', '+')}"
        page.get(search_url)

        if not _wait_for_cf(page):
            return {"error": "Cloudflare challenge could not be solved", "results": []}

        html = page.html

        # Extract game listings from search results
        listings = []
        seen = set()
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

            listings.append({
                "title": name,
                "url": href,
                "platform": detect_platform(name, href)
            })
            if len(listings) >= max_results:
                break

        # Step 2: Fetch details for top N results using same browser session
        results = []
        to_fetch = min(len(listings), fetch_details)

        for i in range(to_fetch):
            listing = listings[i]
            try:
                page.get(listing["url"])

                # CF should already be cleared (cookies persist in session)
                # But check title just in case
                for j in range(10):
                    page.wait(1.5)
                    title = str(page.title or "")
                    if ("Just a moment" not in title and
                        "siamo" not in title.lower() and
                        j > 0):
                        page.wait(2)
                        break

                detail_html = page.html

                # Extract metadata
                game_title = listing["title"]
                m = re.search(r"<h1[^>]*>([^<]+)</h1>", detail_html, re.I)
                if m:
                    game_title = m.group(1).strip()

                pkg_match = re.search(
                    r"((?:PPSA|CUSA)\d{5})\s*[-\u2013]?\s*(USA|EUR|JPN|ASIA)?",
                    detail_html, re.I
                )
                region = pkg_match.group(2).upper() if pkg_match and pkg_match.group(2) else ""
                pkg_id = pkg_match.group(1) if pkg_match else ""

                fw_match = re.search(
                    r"(?:Works\s+on|Firmware|FW|Requires?)[^0-9]*(\d+\.\d+)",
                    detail_html, re.I
                )
                firmware = fw_match.group(1) if fw_match else ""

                pw_match = re.search(r"Password\s*:\s*([^\r\n<]+)", detail_html, re.I)
                password = pw_match.group(1).strip() if pw_match else ""

                # Extract download links from rendered page
                all_links = extract_download_links_from_page(page)
                sections = parse_download_sections(detail_html, all_links)

                result = {
                    "title": game_title,
                    "url": listing["url"],
                    "platform": listing["platform"],
                    "pkg_id": pkg_id,
                    "region": region,
                    "firmware": firmware,
                    "password": password,
                    "links": all_links,
                    "sections": sections
                }
                results.append(result)

            except Exception as e:
                # On error, add listing without details
                results.append({
                    "title": listing["title"],
                    "url": listing["url"],
                    "platform": listing["platform"],
                    "links": [],
                    "sections": [],
                    "error": str(e)
                })

        # Add remaining listings (without details)
        for i in range(to_fetch, len(listings)):
            listing = listings[i]
            results.append({
                "title": listing["title"],
                "url": listing["url"],
                "platform": listing["platform"],
                "links": [],
                "sections": []
            })

        return {"results": results}

    except Exception as e:
        return {"error": str(e), "results": []}
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
    parser.add_argument("--fetch-details", "-d", type=int, default=5,
                        help="Number of results to fetch full details for (default: 5)")
    parser.add_argument("--fetch-page", type=str, help="Fetch a single game detail page URL")
    parser.add_argument("--no-details", action="store_true",
                        help="Skip fetching game page details (listing only)")
    args = parser.parse_args()

    if args.fetch_page:
        result = fetch_game_page(args.fetch_page)
        print(json.dumps(result, ensure_ascii=False))
    elif args.query:
        if args.no_details:
            result = search_listings(args.query, args.max)
        else:
            result = full_search(args.query, args.max, args.fetch_details)
        print(json.dumps(result, ensure_ascii=False))
    else:
        parser.print_help()
        sys.exit(1)


if __name__ == "__main__":
    main()
