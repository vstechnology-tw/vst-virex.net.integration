import json
import sys
import urllib.request


def request_json(method, url, payload=None):
    data = None
    headers = {}
    if payload is not None:
        data = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json"

    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    with urllib.request.urlopen(request, timeout=10) as response:
        body = response.read().decode("utf-8")
        return json.loads(body) if body else None


def main():
    base_url = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://127.0.0.1:5088"

    status = request_json("GET", f"{base_url}/api/status")
    print(
        "Status: "
        f"initialized={status.get('initialized')}, "
        f"processState={status.get('processState')}, "
        f"recipe={status.get('recipe')}"
    )

    wafer_info = {
        "lotId": "LOT-PY-REST-001",
        "waferId": "W01",
        "recipeId": "RCP-A",
        "slot": "1",
        "foupId": "FOUP-A",
        "chamberId": "CH-1",
    }

    request_json("POST", f"{base_url}/api/wafer-info", wafer_info)
    print("WaferInfo updated through raw REST.")


if __name__ == "__main__":
    main()

