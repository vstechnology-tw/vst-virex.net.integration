import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request


PRODUCT_INFO = {
    "waferID": "WPY-REST-210-001",
    "lotID": "LOT-PY-REST-210",
    "recipe": "RCP-DEMO",
    "slot": "1",
    "foupID": "FOUP-DEMO",
    "chamberID": "CH-1",
}


def prompt(message):
    print()
    print("Action required in Simulator:")
    input(message + " ")


def print_step(title):
    print()
    print(f"== {title} ==")


def request_json(method, url, payload=None):
    data = None
    headers = {}
    if payload is not None:
        data = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        headers["Content-Type"] = "application/json"

    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    with urllib.request.urlopen(request, timeout=10) as response:
        body = response.read().decode("utf-8")
        print(body)
        return json.loads(body) if body else None


def main():
    base_url = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://127.0.0.1:5088"

    print_step("Virex.NET Python Raw RESTful API 13-Step Demo")
    print(f"RESTful API base URL: {base_url}")
    prompt("Press Start Servers, then press Enter here.")

    try:
        print_step("Step 1 - Query status")
        request_json("GET", f"{base_url}/api/status")

        print_step("Step 2 - Query error")
        request_json("GET", f"{base_url}/api/error")

        print_step("Step 3 - Query ProductInfo")
        request_json("GET", f"{base_url}/api/product-info")

        print_step("Step 4 - Initialize")
        request_json("POST", f"{base_url}/api/system/initialize")

        print_step("Step 5 - Confirm Ready")
        request_json("GET", f"{base_url}/api/status")

        print_step("Step 6 - Set ProductInfo")
        request_json("POST", f"{base_url}/api/product-info", PRODUCT_INFO)

        print_step("Step 7 - Confirm ProductInfo")
        request_json("GET", f"{base_url}/api/product-info")

        print_step("Step 8 - Start run")
        request_json("POST", f"{base_url}/api/system/start", {"condition": "golden-sample", "runMode": "continue"})

        print_step("Step 9 - Observe run events")
        print("RESTful API has no event stream; waiting briefly, then polling status/results.")
        time.sleep(1.2)
        request_json("GET", f"{base_url}/api/status")

        print_step("Step 10 - Stop run")
        request_json("POST", f"{base_url}/api/system/stop", {"reason": "operator-request"})

        print_step("Step 11 - Query results")
        query = urllib.parse.urlencode({"lotID": PRODUCT_INFO["lotID"], "waferID": PRODUCT_INFO["waferID"]})
        request_json("GET", f"{base_url}/api/results?{query}")

        print_step("Step 12 - Deinitialize")
        request_json("POST", f"{base_url}/api/system/deinitialize")

        print_step("Step 13 - Confirm Uninitialized")
        request_json("GET", f"{base_url}/api/status")
    except urllib.error.URLError as error:
        print("Connection failed. In Simulator, press Start Servers and verify the RESTful API endpoint matches this sample.")
        print(error)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
