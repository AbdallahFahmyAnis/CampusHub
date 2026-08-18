import { isAdmin, isStaff } from "./auth.js";
import { courseRoomId, parseCourseId } from "./store.js";

const catalogBase = process.env.CATALOG_BASE_URL ?? "http://localhost:5102";
const enrollmentBase = process.env.ENROLLMENT_BASE_URL ?? "http://localhost:5103";

export async function listAccessibleRooms(user) {
  const rooms = [{ id: "campus", title: "Campus lobby", kind: "campus" }];
  const courses = isAdmin(user)
    ? await getJson(`${catalogBase}/api/catalog/courses`, user.accessToken)
    : isStaff(user)
      ? await getJson(`${catalogBase}/api/catalog/courses/mine`, user.accessToken)
      : confirmedCourses(await getJson(`${enrollmentBase}/api/enrollments/mine`, user.accessToken));

  for (const course of courses) {
    rooms.push({
      id: courseRoomId(course.id ?? course.courseId),
      title: course.title ?? course.courseTitle,
      kind: "course",
      courseId: course.id ?? course.courseId,
    });
  }
  return rooms;
}

export async function canJoinRoom(user, roomId) {
  if (roomId === "campus") {
    return { ok: true, title: "Campus lobby" };
  }

  const courseId = parseCourseId(roomId);
  if (!courseId) {
    return { ok: false, reason: "Unknown room." };
  }

  if (isStaff(user)) {
    const course = await getJson(`${catalogBase}/api/catalog/courses/${courseId}`, user.accessToken);
    return { ok: true, title: course.title };
  }

  const enrollments = await getJson(`${enrollmentBase}/api/enrollments/mine`, user.accessToken);
  const match = enrollments.find(
    (item) => item.courseId === courseId && item.status === "Confirmed",
  );
  if (!match) {
    return { ok: false, reason: "Join a course before entering its chat." };
  }
  return { ok: true, title: match.courseTitle };
}

function confirmedCourses(enrollments) {
  const seen = new Set();
  const courses = [];
  for (const item of enrollments ?? []) {
    if (item.status !== "Confirmed" || seen.has(item.courseId)) {
      continue;
    }
    seen.add(item.courseId);
    courses.push({ id: item.courseId, title: item.courseTitle });
  }
  return courses;
}

async function getJson(url, accessToken) {
  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    throw new Error(`Upstream ${url} returned ${response.status}`);
  }
  return response.json();
}
