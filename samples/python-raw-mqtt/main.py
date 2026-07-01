import json
import socket
import sys
import time


PRODUCT_INFO = {
    "waferID": "WPY-MQTT-210-001",
    "lotID": "LOT-PY-MQTT-210",
    "recipe": "RCP-DEMO",
    "slot": "1",
    "foupID": "FOUP-DEMO",
    "chamberID": "CH-1",
}


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


def publish_packet(topic, message):
    payload = utf8_field(topic) + message.encode("utf-8")
    return packet(0x30, payload)


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


def read_publish_until(client, expected_topic=None, timeout_seconds=5):
    deadline = time.monotonic() + timeout_seconds
    while time.monotonic() < deadline:
        try:
            first, payload = recv_packet(client)
        except socket.timeout:
            continue

        if (first >> 4) != 3:
            continue

        topic, message = parse_publish(first, payload)
        print(f"{topic}: {message}")
        if expected_topic is None or topic == expected_topic:
            return message

    raise TimeoutError(f"MQTT topic was not observed: {expected_topic or 'any event'}")


def publish_command(client, base_topic, command_topic, correlation_id, **payload):
    request = {"correlationId": correlation_id}
    request.update(payload)
    command_payload = json.dumps(request, separators=(",", ":"))
    topic = f"{base_topic}/{command_topic}"
    response_topic = f"{base_topic}/responses/{correlation_id}"
    client.sendall(publish_packet(topic, command_payload))
    print(f"Published {topic}: {command_payload}")
    return read_publish_until(client, response_topic)


def main():
    host = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 1883
    base_topic = sys.argv[3] if len(sys.argv) > 3 else "virex"
    topic_filter = f"{base_topic}/#"

    print_step("Virex.NET Python Raw MQTT 13-Step Demo")
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

        print_step("Step 1 - Query status")
        publish_command(client, base_topic, "commands/status/get", "python-raw-mqtt-status-1")

        print_step("Step 2 - Query error")
        publish_command(client, base_topic, "commands/error/get", "python-raw-mqtt-error-2")

        print_step("Step 3 - Query ProductInfo")
        publish_command(client, base_topic, "commands/product-info/get", "python-raw-mqtt-product-get-3")

        print_step("Step 4 - Initialize")
        publish_command(client, base_topic, "commands/system/initialize", "python-raw-mqtt-initialize-4")

        print_step("Step 5 - Confirm Ready")
        publish_command(client, base_topic, "commands/status/get", "python-raw-mqtt-ready-5")

        print_step("Step 6 - Set ProductInfo")
        publish_command(client, base_topic, "commands/product-info/set", "python-raw-mqtt-product-set-6", productInfo=PRODUCT_INFO)

        print_step("Step 7 - Confirm ProductInfo")
        publish_command(client, base_topic, "commands/product-info/get", "python-raw-mqtt-product-confirm-7")

        print_step("Step 8 - Start run")
        publish_command(client, base_topic, "commands/system/start", "python-raw-mqtt-start-8", condition="golden-sample", runMode="continue")

        print_step("Step 9 - Observe run events")
        read_publish_until(client, timeout_seconds=2)

        print_step("Step 10 - Stop run")
        publish_command(client, base_topic, "commands/system/stop", "python-raw-mqtt-stop-10", reason="operator-request")

        print_step("Step 11 - Query results")
        publish_command(client, base_topic, "commands/results/query", "python-raw-mqtt-results-11", lotID=PRODUCT_INFO["lotID"], waferID=PRODUCT_INFO["waferID"])

        print_step("Step 12 - Deinitialize")
        publish_command(client, base_topic, "commands/system/deinitialize", "python-raw-mqtt-deinitialize-12")

        print_step("Step 13 - Confirm Uninitialized")
        publish_command(client, base_topic, "commands/status/get", "python-raw-mqtt-uninitialized-13")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
