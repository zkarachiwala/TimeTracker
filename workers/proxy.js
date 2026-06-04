/**
 * Cloudflare Worker — App Service reverse proxy
 *
 * Routes timetracker.dzk.com.au to timetracker-zak.azurewebsites.net.
 * App Service F1 does not support custom domain bindings, so it rejects
 * requests with Host: timetracker.dzk.com.au. This Worker rewrites the
 * outbound request URL to the azurewebsites.net origin, which causes the
 * Workers runtime to set Host automatically to that value.
 */

const ORIGIN = "https://timetracker-zak.azurewebsites.net";

export default {
  async fetch(request) {
    const url = new URL(request.url);
    const originUrl = ORIGIN + url.pathname + url.search;

    // Strip the incoming Host header — the runtime sets it from the URL above.
    // Preserve it as X-Forwarded-Host so ASP.NET Core uses the public hostname
    // for OAuth callbacks and redirects (required for Google OAuth to work).
    const headers = new Headers(request.headers);
    headers.set("x-forwarded-host", url.hostname);
    headers.delete("host");

    if (request.headers.get("Upgrade")?.toLowerCase() === "websocket") {
      return proxyWebSocket(originUrl, headers);
    }

    return fetch(new Request(originUrl, {
      method: request.method,
      headers,
      body: request.body,
      redirect: "follow",
    }));
  },
};

async function proxyWebSocket(originUrl, headers) {
  const wsOriginUrl = originUrl.replace(/^https/, "wss");

  const backendResponse = await fetch(wsOriginUrl, {
    headers,
    cf: { websocket: true },
  });

  const backendWs = backendResponse.webSocket;
  if (!backendWs) {
    return new Response("Backend did not accept WebSocket upgrade", { status: 502 });
  }
  backendWs.accept();

  const [clientSocket, serverSocket] = Object.values(new WebSocketPair());
  serverSocket.accept();

  serverSocket.addEventListener("message", ({ data }) => backendWs.send(data));
  backendWs.addEventListener("message", ({ data }) => serverSocket.send(data));

  serverSocket.addEventListener("close", ({ code, reason }) => backendWs.close(code, reason));
  backendWs.addEventListener("close", ({ code, reason }) => serverSocket.close(code, reason));

  serverSocket.addEventListener("error", () => backendWs.close());
  backendWs.addEventListener("error", () => serverSocket.close());

  return new Response(null, { status: 101, webSocket: clientSocket });
}
