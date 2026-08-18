import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.dirname(fileURLToPath(import.meta.url));
const dataDir = process.env.DATA_DIR ?? path.join(root, "..", "data");
const storePath = path.join(dataDir, "chat-store.json");

const campusRoom = {
  id: "campus",
  title: "Campus lobby",
  kind: "campus",
};

export class ChatStore {
  constructor() {
    this.rooms = new Map([[campusRoom.id, { ...campusRoom, messages: [] }]]);
    this.writeQueue = Promise.resolve();
  }

  async load() {
    try {
      const raw = await readFile(storePath, "utf8");
      const parsed = JSON.parse(raw);
      this.rooms = new Map(
        (parsed.rooms ?? []).map((room) => [room.id, { ...room, messages: room.messages ?? [] }]),
      );
      if (!this.rooms.has(campusRoom.id)) {
        this.rooms.set(campusRoom.id, { ...campusRoom, messages: [] });
      }
    } catch (error) {
      if (error.code !== "ENOENT") {
        console.warn("Could not load chat store, starting empty.", error.message);
      }
    }
  }

  ensureRoom(id, title, kind = "course") {
    if (!this.rooms.has(id)) {
      this.rooms.set(id, { id, title, kind, messages: [] });
    } else if (title) {
      this.rooms.get(id).title = title;
    }
    return this.rooms.get(id);
  }

  recent(roomId, limit = 50) {
    const room = this.rooms.get(roomId);
    if (!room) {
      return [];
    }
    return room.messages.slice(-limit);
  }

  async append(message) {
    const room = this.ensureRoom(message.roomId, message.roomTitle, message.roomId === "campus" ? "campus" : "course");
    if (message.clientId && room.messages.some((item) => item.clientId === message.clientId)) {
      return room.messages.find((item) => item.clientId === message.clientId);
    }
    room.messages.push(message);
    if (room.messages.length > 500) {
      room.messages.splice(0, room.messages.length - 500);
    }
    await this.persist();
    return message;
  }

  persist() {
    this.writeQueue = this.writeQueue.then(async () => {
      await mkdir(dataDir, { recursive: true });
      const payload = {
        rooms: [...this.rooms.values()].map((room) => ({
          id: room.id,
          title: room.title,
          kind: room.kind,
          messages: room.messages,
        })),
      };
      await writeFile(storePath, JSON.stringify(payload, null, 2), "utf8");
    });
    return this.writeQueue;
  }
}

export function courseRoomId(courseId) {
  return `course:${courseId}`;
}

export function parseCourseId(roomId) {
  return roomId.startsWith("course:") ? roomId.slice("course:".length) : null;
}
