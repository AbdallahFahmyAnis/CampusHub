import { isAdmin, isStaff } from "./auth.js";
import { campusRoomId, courseRoomId, parseCourseId, parseTutorCourseId, tutorRoomId } from "./store.js";

const catalogBase = process.env.CATALOG_BASE_URL ?? "http://localhost:5102";
const enrollmentBase = process.env.ENROLLMENT_BASE_URL ?? "http://localhost:5103";

function allowsChat(user) {
  return (user.plan ?? "campus").toLowerCase() !== "free";
}

export async function listAccessibleRooms(user) {
  if (!allowsChat(user)) {
    return [];
  }

  const rooms = [{
    id: campusRoomId(user.tenantId),
    title: "Campus lobby",
    kind: "campus",
  }];
  const courses = isAdmin(user)
    ? (await getJson(`${catalogBase}/api/catalog/courses?pageSize=100`, user.accessToken)).items ?? []
    : isStaff(user)
      ? await getJson(`${catalogBase}/api/catalog/courses/mine`, user.accessToken)
      : confirmedCourses(await getJson(`${enrollmentBase}/api/enrollments/mine`, user.accessToken));

  for (const course of courses) {
    const cId = course.id ?? course.courseId;
    const cTitle = course.title ?? course.courseTitle;
    rooms.push({
      id: courseRoomId(cId),
      title: cTitle,
      kind: "course",
      courseId: cId,
    });
    rooms.push({
      id: tutorRoomId(cId),
      title: `AI Tutor — ${cTitle}`,
      kind: "tutor",
      courseId: cId,
    });
  }
  return rooms;
}

export async function askTutor(courseId, question, accessToken) {
  const response = await fetch(`${catalogBase}/api/catalog/courses/${courseId}/ask`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({ question }),
  });
  if (!response.ok) {
    throw new Error(`Catalog /ask returned ${response.status}`);
  }
  const data = await response.json();
  return data.answer ?? "I could not find an answer for that.";
}

export async function canJoinRoom(user, roomId) {
  if (!allowsChat(user)) {
    return { ok: false, reason: "Live chat requires the Campus plan. Upgrade in Billing." };
  }

  const lobbyId = campusRoomId(user.tenantId);
  if (roomId === lobbyId || roomId === "campus") {
    return { ok: true, title: "Campus lobby" };
  }

  const tutorCourseId = parseTutorCourseId(roomId);
  if (tutorCourseId) {
    if (isStaff(user)) {
      const course = await getJson(`${catalogBase}/api/catalog/courses/${tutorCourseId}`, user.accessToken);
      return { ok: true, title: `AI Tutor — ${course.title}`, isTutor: true, courseId: tutorCourseId };
    }
    const enrollments = await getJson(`${enrollmentBase}/api/enrollments/mine`, user.accessToken);
    const match = enrollments.find((item) => item.courseId === tutorCourseId && item.status === "Confirmed");
    if (!match) {
      return { ok: false, reason: "Enroll in this course to access the AI tutor." };
    }
    return { ok: true, title: `AI Tutor — ${match.courseTitle}`, isTutor: true, courseId: tutorCourseId };
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
