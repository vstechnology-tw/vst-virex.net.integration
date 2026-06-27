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
    print("This sample connects to the simulator TCP socket, reads initial frames, sends ProductInfo, and sends start/stop commands.")
    print(f"TCP endpoint: {host}:{port}")
    prompt("Press Start Servers and Initialize, then press Enter here.")

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
        print("Initial ProductInfo frame:")
        print(read_line(file))

        print_step("Step 2 - Send productInfo frame")
        frame = {
            "type": "productInfo",
            "waferID": "W01",
            "lotID": "LOT-PY-TCP-001",
            "recipe": "RCP-A",
            "slot": "1",
            "foupID": "FOUP-A",
            "chamberID": "CH-1",
        }

        client.sendall((json.dumps(frame, separators=(",", ":")) + "\n").encode("utf-8"))
        print("Sent productInfo frame.")
        print("Expected Simulator Event Log:")
        print("ProductInfo updated from TCP.")
        print("Waiting for echoed productInfo update event...")
        print(read_line(file))

        print_step("Step 3 - Send start command with condition and runMode payload")
        start_frame = {"type": "start", "condition": "golden-sample", "runMode": "continue"}
        client.sendall((json.dumps(start_frame, separators=(",", ":")) + "\n").encode("utf-8"))
        print("Sent start frame:")
        print(json.dumps(start_frame, separators=(",", ":")))
        print("Expected Simulator Event Log:")
        print("Start condition: golden-sample")
        print("Start run mode: continue")
        print("Waiting for status transition...")
        print(read_line(file))

        print_step("Step 4 - Send stop command with reason payload")
        stop_frame = {"type": "stop", "reason": "operator-request"}
        client.sendall((json.dumps(stop_frame, separators=(",", ":")) + "\n").encode("utf-8"))
        print("Sent stop frame:")
        print(json.dumps(stop_frame, separators=(",", ":")))
        print("Expected Simulator Event Log:")
        print("Stopped. reason=operator-request")
        print("Waiting for ready status...")
        print(read_line(file))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

