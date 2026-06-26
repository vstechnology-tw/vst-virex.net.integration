import json
import sys
import threading
import time
import urllib.error
import urllib.request


def prompt(message):
    print()
    print("Action required in Simulator:")
    input(message + " ")


def print_step(title):
    print()
    print(f"== {title} ==")


def request_json(method, url, payload=None, allow_error=False):
    data = None
    headers = {}
    if payload is not None:
        data = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json"

    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=10) as response:
            body = response.read().decode("utf-8")
            return response.status, json.loads(body) if body else None
    except urllib.error.HTTPError as error:
        body = error.read().decode("utf-8")
        if allow_error:
            return error.code, json.loads(body) if body else None
        raise


def print_status(status):
    print(
        "Status: "
        f"initialized={status.get('initialized')}, "
        f"processState={status.get('processState')}, "
        f"recipe={status.get('recipe')}"
    )


def main():
    base_url = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://127.0.0.1:5088"
    lot_id = "LOT-PY-REST-001"

    print_step("Virex.NET Python Raw REST Guided Demo")
    print("This sample uses direct REST calls and shows how simulator UI state affects API returns.")
    print(f"REST base URL: {base_url}")
    prompt("Press Start Servers. Leave Initialize unpressed for the first check, then press Enter here.")

    try:
        print_step("Step 1 - Read /api/status")
        _, status = request_json("GET", f"{base_url}/api/status")
        print_status(status)

        if not status.get("initialized"):
            print_step("Step 2 - Expected negative check")
            print("Calling POST /api/control/start before Initialize should return HTTP 409 not_initialized.")
            code, body = request_json("POST", f"{base_url}/api/control/start", allow_error=True)
            print(f"Start returned HTTP {code}: {json.dumps(body, separators=(',', ':'))}")
            if code != 409 or body.get("message") != "not_initialized":
                raise RuntimeError("Expected HTTP 409 not_initialized. Confirm the simulator was not initialized.")

            prompt("Press Initialize. Confirm Status shows initialized=True, processState=ready, then press Enter here.")
            _, status = request_json("GET", f"{base_url}/api/status")
            print_status(status)

        print_step("Step 3 - POST /api/wafer-info")
        wafer_info = {
            "lotId": lot_id,
            "waferId": "W01",
            "recipeId": "RCP-A",
            "slot": "1",
            "foupId": "FOUP-A",
            "chamberId": "CH-1",
        }

        request_json("POST", f"{base_url}/api/wafer-info", wafer_info)
        print("Expected Simulator Event Log:")
        print("WaferInfo updated from REST: lotId=LOT-PY-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1")

        print_step("Step 4 - POST /api/control/start with condition and runMode payload")
        print("Expected Simulator Status: capturing -> inspecting -> saving -> ready.")
        code, body = request_json("POST", f"{base_url}/api/control/start", {"condition": "golden-sample", "runMode": "continue"}, allow_error=True)
        print(f"Start returned HTTP {code}: {json.dumps(body, separators=(',', ':'))}")
        if code >= 400:
            raise RuntimeError("Start failed. Confirm Initialize was pressed and processState is ready.")
        print("Expected Simulator Event Log:")
        print("Start condition: golden-sample")
        print("Start run mode: continue")

        print_step("Step 5 - POST /api/control/stop with reason payload")
        start_error = []

        def start_for_stop():
            try:
                request_json("POST", f"{base_url}/api/control/start", {"condition": "stop-demo", "runMode": "continue"}, allow_error=True)
            except Exception as error:
                start_error.append(error)

        start_thread = threading.Thread(target=start_for_stop)
        start_thread.start()
        time.sleep(0.3)
        code, body = request_json("POST", f"{base_url}/api/control/stop", {"reason": "operator-request"}, allow_error=True)
        start_thread.join()
        if start_error:
            raise start_error[0]
        print(f"Stop returned HTTP {code}: {json.dumps(body, separators=(',', ':'))}")
        if code >= 400:
            raise RuntimeError("Stop failed. Confirm a cycle was running.")
        print("Expected Simulator Event Log:")
        print("Stopped. reason=operator-request")

        print_step("Step 6 - GET /api/results by lotId")
        _, results = request_json("GET", f"{base_url}/api/results?lotId={lot_id}")
        print(f"Result count for {lot_id}: {results.get('count')}")
    except urllib.error.URLError as error:
        print("Connection failed. In Simulator, press Start Servers and verify the REST endpoint matches this sample.")
        print(error)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
