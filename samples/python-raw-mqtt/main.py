import socket
import sys
import time


def encode_remaining_length(value):
    encoded = bytearray()
    while True:
        digit = value % 128
        value //= 128
        if value > 0:
            digit |= 0x80
        encoded.append(digit)
        if value == 0:
            return bytes(encoded)


def utf8_field(value):
    data = value.encode("utf-8")
    return len(data).to_bytes(2, "big") + data


def packet(packet_type, payload):
    return bytes([packet_type]) + encode_remaining_length(len(payload)) + payload


def recv_exact(client, length):
    chunks = bytearray()
    while len(chunks) < length:
        chunk = client.recv(length - len(chunks))
        if not chunk:
            raise RuntimeError("MQTT connection closed.")
        chunks.extend(chunk)
    return bytes(chunks)


def recv_packet(client):
    first = recv_exact(client, 1)[0]
    multiplier = 1
    remaining = 0
    while True:
        digit = recv_exact(client, 1)[0]
        remaining += (digit & 0x7F) * multiplier
        if (digit & 0x80) == 0:
            break
        multiplier *= 128
    return first, recv_exact(client, remaining)


def connect_packet(client_id):
    variable_header = utf8_field("MQTT") + bytes([4, 2]) + (60).to_bytes(2, "big")
    payload = utf8_field(client_id)
    return packet(0x10, variable_header + payload)


def subscribe_packet(packet_id, topic):
    payload = packet_id.to_bytes(2, "big") + utf8_field(topic) + bytes([0])
    return packet(0x82, payload)


def parse_publish(first, payload):
    qos = (first >> 1) & 0x03
    topic_length = int.from_bytes(payload[0:2], "big")
    topic = payload[2 : 2 + topic_length].decode("utf-8")
    offset = 2 + topic_length
    if qos:
        offset += 2
    message = payload[offset:].decode("utf-8")
    return topic, message


def print_step(title):
    print()
    print(f"== {title} ==")


def prompt(message):
    print()
    print("Action required in Simulator:")
    input(message + " ")


def main():
    host = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 1883
    base_topic = sys.argv[3] if len(sys.argv) > 3 else "virex"
    duration_seconds = int(sys.argv[4]) if len(sys.argv) > 4 else 30
    topic_filter = f"{base_topic}/#"

    print_step("Virex.NET Python Raw MQTT Guided Demo")
    print("This sample subscribes to simulator MQTT events and lets you trigger each event from the UI.")
    print(f"MQTT endpoint: {host}:{port}")
    print(f"Topic filter: {topic_filter}")
    prompt("Press Start Servers, then press Enter here.")

    try:
        client = socket.create_connection((host, port), timeout=10)
    except OSError as error:
        print("Connection failed. In Simulator, press Start Servers and verify the MQTT host, port, and topic.")
        print(error)
        return 1

    with client:
        client.settimeout(1)
        client.sendall(connect_packet("virex-python-raw-mqtt"))
        first, payload = recv_packet(client)
        if first != 0x20 or len(payload) < 2 or payload[1] != 0:
            raise RuntimeError("MQTT CONNACK did not indicate success.")

        client.sendall(subscribe_packet(1, topic_filter))
        first, _ = recv_packet(client)
        if first != 0x90:
            raise RuntimeError("MQTT SUBACK was not received.")

        print_step("Step 1 - Trigger events from Simulator")
        print(f"Subscribed to {topic_filter} for {duration_seconds} seconds.")
        print("Expected UI actions and topics:")
        print("- Press Apply WaferInfo: expect virex/wafer-info.")
        print("- Press Initialize: expect virex/status with initialized=true.")
        print("- Press Start Cycle: expect virex/status transitions.")
        print("- Press Emit Fake Result: expect virex/result.")
        print("- Press Emit Error: expect virex/error.")

        deadline = time.monotonic() + duration_seconds
        while time.monotonic() < deadline:
            try:
                first, payload = recv_packet(client)
            except socket.timeout:
                continue

            if (first >> 4) == 3:
                topic, message = parse_publish(first, payload)
                print(f"{topic}: {message}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

