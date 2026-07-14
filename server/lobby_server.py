import socket
import threading
import random
import string
import sys

HOST = "127.0.0.1"
PORT = 7700
GAME_SERVER_BASE_PORT = 7777

lobbies = {}
next_port = GAME_SERVER_BASE_PORT


def generate_code(length=5):
    return ''.join(random.choices(string.ascii_uppercase + string.digits, k=length))


def handle_client(conn, addr):
    global next_port
    try:
        data = conn.recv(1024).decode("utf-8").strip()
        print(f"[{addr}] Received: {data}")

        if data == "CREATE":
            code = generate_code()
            port = next_port
            next_port += 1
            lobbies[code] = port
            response = f"CODE:{code}\nPORT:{port}\n"
            conn.sendall(response.encode("utf-8"))
            print(f"[{addr}] Created lobby {code} on port {port}")

        elif data.startswith("JOIN:"):
            code = data.split(":", 1)[1].strip().upper()
            if code in lobbies:
                port = lobbies[code]
                response = f"PORT:{port}\n"
                conn.sendall(response.encode("utf-8"))
                print(f"[{addr}] Joined lobby {code} on port {port}")
            else:
                conn.sendall(b"NOT_FOUND\n")
                print(f"[{addr}] Lobby {code} not found")
        else:
            conn.sendall(b"ERROR:UNKNOWN_COMMAND\n")
            print(f"[{addr}] Unknown command: {data}")
    except Exception as e:
        print(f"[{addr}] Error: {e}")
    finally:
        conn.close()


def main():
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((HOST, PORT))
    server.listen(5)
    print(f"Lobby server listening on {HOST}:{PORT}")

    try:
        while True:
            conn, addr = server.accept()
            t = threading.Thread(target=handle_client, args=(conn, addr), daemon=True)
            t.start()
    except KeyboardInterrupt:
        print("\nShutting down...")
    finally:
        server.close()


if __name__ == "__main__":
    main()
