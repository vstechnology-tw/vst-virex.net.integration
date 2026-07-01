import json
import socket
import sys


PRODUCT_INFO = {
    "type": "productInfo",
    "waferID": "WPY-TCP-210-001",
    "lotID": "LOT-PY-TCP-210",
    "recipe": "RCP-DEMO",
    "slot": "1",
    "foupID": "FOUP-DEMO",
    "chamberID": "CH-1",
}


def read_line(file):
    line = file.readline()
    if not line:
        raise RuntimeError("Connection closed before a full TCP frame was received.")
    return line.decode("utf-8").rstrip("\n")


def send_frame(client, frame):
    client.sendall((json.dumps(frame, separators=(",", ":")) + "\n").encode("utf-8"))


def print_step(title):
    print()
    print(f"== {title} ==")


def prompt(message):
    print()
    print("Action required in Simulator:")
    input(message + " ")


def main():
    host = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 5089

    print_step("Virex.NET Python Raw TCP 13-Step Demo")
    print(f"TCP endpoint: {host}:{port}")
    prompt("Press Start Servers, then press Enter here.")

    try:
        client = socket.create_connection((host, port), timeout=10)
    except OSError as error:
        print("Connection failed. In Simulator, press Start Servers and verify the TCP port matches this sample.")
        print(error)
        return 1

    with client:
        file = client.makefile("rb")
        print("Initial simulator frames:")
        print(read_line(file))
        print(read_line(file))

        print_step("Step 1 - Query status")
        send_frame(client, {"type": "status"})
        print(read_line(file))

        print_step("Step 2 - Query error")
        send_frame(client, {"type": "error"})
        print(read_line(file))

        print_step("Step 3 - Query ProductInfo")
        send_frame(client, {"type": "getProductInfo"})
        print(read_line(file))

        print_step("Step 4 - Initialize")
        send_frame(client, {"type": "initialize"})
        print(read_line(file))

        print_step("Step 5 - Confirm Ready")
        print(read_line(file))

        print_step("Step 6 - Set ProductInfo")
        send_frame(client, PRODUCT_INFO)
        print(read_line(file))

        print_step("Step 7 - Confirm ProductInfo")
        send_frame(client, {"type": "getProductInfo"})
        print(read_line(file))

        print_step("Step 8 - Start run")
        send_frame(client, {"type": "start", "condition": "golden-sample", "runMode": "continue"})
        print(read_line(file))

        print_step("Step 9 - Observe run events")
        print(read_line(file))

        print_step("Step 10 - Stop run")
        send_frame(client, {"type": "stop", "reason": "operator-request"})
        print(read_line(file))

        print_step("Step 11 - Query results")
        send_frame(client, {"type": "results", "lotID": PRODUCT_INFO["lotID"], "waferID": PRODUCT_INFO["waferID"]})
        print(read_line(file))

        print_step("Step 12 - Deinitialize")
        send_frame(client, {"type": "deinitialize"})
        print(read_line(file))

        print_step("Step 13 - Confirm Uninitialized")
        print(read_line(file))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
