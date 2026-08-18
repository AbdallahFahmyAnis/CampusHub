import http from "node:http";
import express from "express";
import { Server } from "socket.io";
import { verifyAccessToken } from "./auth.js";
import { canJoinRoom, listAccessibleRooms } from "./access.js";
import { ChatStore } from "./store.js";

const port = Number(process.env.PORT ?? 5107);
const store = new ChatStore();
await store.load();

const app = express();
app.use(express.json({ limit: "8kb" }));

app.get("/health/live", (_req, res) => res.json({ status: "live" }));
app.get("/health/ready", (_req, res) => res.json({ status: "ready" }));

app.use("/api/chat", async (req, res, next) => {
  try {
    req.user = await verifyAccessToken(req.headers.authorization);
    next();
  } catch {
    res.status(401).json({ error: "Unauthorized" });
  }
});

app.get("/api/chat/rooms", async (req, res) => {
  try {
    res.json(await listAccessibleRooms(req.user));
  } catch (error) {
    res.status(502).json({ error: error.message });
  }
});

app.get("/api/chat/rooms/:roomId/messages", async (req, res) => {
  try {
    const access = await canJoinRoom(req.user, req.params.roomId);
    if (!access.ok) {
      return res.status(403).json({ error: access.reason });
    }
    store.ensureRoom(req.params.roomId, access.title, req.params.roomId === "campus" ? "campus" : "course");
    res.json(store.recent(req.params.roomId, Number(req.query.limit ?? 50)));
  } catch (error) {
    res.status(502).json({ error: error.message });
  }
});

const server = http.createServer(app);
const io = new Server(server, {
  cors: {
    origin: ["http://localhost:5000", "http://localhost:4200"],
    credentials: true,
  },
});

io.use(async (socket, next) => {
  try {
    socket.user = await verifyAccessToken(socket.handshake.headers.authorization);
    next();
  } catch (error) {
    next(new Error("Unauthorized"));
  }
});

io.on("connection", (socket) => {
  socket.on("join", async (roomId, ack) => {
    try {
      const access = await canJoinRoom(socket.user, roomId);
      if (!access.ok) {
        ack?.({ ok: false, error: access.reason });
        return;
      }
      store.ensureRoom(roomId, access.title, roomId === "campus" ? "campus" : "course");
      socket.join(roomId);
      ack?.({ ok: true, title: access.title, messages: store.recent(roomId, 50) });
    } catch (error) {
      ack?.({ ok: false, error: error.message });
    }
  });

  socket.on("message", async (payload, ack) => {
    try {
      const roomId = payload?.roomId;
      const body = String(payload?.body ?? "").trim();
      if (!roomId || !body) {
        ack?.({ ok: false, error: "Room and message are required." });
        return;
      }
      if (body.length > 2000) {
        ack?.({ ok: false, error: "Message is too long." });
        return;
      }
      if (!socket.rooms.has(roomId)) {
        ack?.({ ok: false, error: "Join the room before sending." });
        return;
      }

      const message = await store.append({
        id: crypto.randomUUID(),
        clientId: payload.clientId ?? crypto.randomUUID(),
        roomId,
        roomTitle: store.rooms.get(roomId)?.title,
        body,
        senderId: socket.user.id,
        senderName: socket.user.name,
        sentAt: new Date().toISOString(),
      });
      io.to(roomId).emit("message", message);
      ack?.({ ok: true, message });
    } catch (error) {
      ack?.({ ok: false, error: error.message });
    }
  });
});

server.listen(port, () => {
  console.log(`CampusHub chat-realtime listening on http://localhost:${port}`);
});
