import json
import socket
import sys


def read_line(file):
    line = file.readline()
    if not line:
        raise RuntimeError("Connection closed before a full TCP frame was received.")
    return line.decode("utf-8").rstrip("\n")


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

    print_step("Virex.NET Python Raw TCP Guided Demo")
    print("This sample connects to the simulator TCP socket, reads initial frames, sends WaferInfo, and waits for the update event.")
    print(f"TCP endpoint: {host}:{port}")
    prompt("Press Start Servers, then press Enter here. Initialize is not required for the WaferInfo TCP demo.")

    try:
        client = socket.create_connection((host, port), timeout=10)
    except OSError as error:
        print("Connection failed. In Simulator, press Start Servers and verify the TCP port matches this sample.")
        print(error)
        return 1

    with client:
        file = client.makefile("rb")

        print_step("Step 1 - Read initial TCP frames")
        print("Initial status frame:")
        print(read_line(file))
        print("Initial WaferInfo frame:")
        print(read_line(file))

        print_step("Step 2 - Send waferInfo frame")
        frame = {
            "type": "waferInfo",
            "lotId": "LOT-PY-TCP-001",
            "waferId": "W01",
            "recipeId": "RCP-A",
            "slot": "1",
            "foupId": "FOUP-A",
            "chamberId": "CH-1",
        }

        client.sendall((json.dumps(frame, separators=(",", ":")) + "\n").encode("utf-8"))
        print("Sent waferInfo frame.")
        print("Expected Simulator Event Log:")
        print("WaferInfo updated from TCP: lotId=LOT-PY-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1")
        print("Waiting for echoed waferInfo update event...")
        print(read_line(file))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

