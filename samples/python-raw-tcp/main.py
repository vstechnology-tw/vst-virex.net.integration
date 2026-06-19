import json
import socket
import sys


def read_line(file):
    line = file.readline()
    if not line:
        raise RuntimeError("Connection closed before a full TCP frame was received.")
    return line.decode("utf-8").rstrip("\n")


def main():
    host = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 5089

    with socket.create_connection((host, port), timeout=10) as client:
        file = client.makefile("rb")

        print(read_line(file))
        print(read_line(file))

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
        print("Sent waferInfo frame. Waiting for echo/update event...")
        print(read_line(file))


if __name__ == "__main__":
    main()

