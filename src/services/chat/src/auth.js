import { createRemoteJWKSet, jwtVerify } from "jose";

const authority = (process.env.IDENTITY_AUTHORITY ?? "http://localhost:5101").replace(/\/$/, "");
const issuer = (process.env.IDENTITY_ISSUER ?? `${authority}/`).replace(/\/?$/, "/");
const jwksUri = process.env.JWKS_URI ?? `${authority}/.well-known/jwks`;
const JWKS = createRemoteJWKSet(new URL(jwksUri));

export async function verifyAccessToken(authorizationHeader) {
  const token = extractBearer(authorizationHeader);
  if (!token) {
    throw new Error("Missing bearer token.");
  }

  const { payload } = await jwtVerify(token, JWKS, { issuer });
  const audiences = payload.aud == null ? [] : Array.isArray(payload.aud) ? payload.aud : [payload.aud];
  if (!audiences.some((value) => value === "chat-api" || value === "campushub-gateway")) {
    throw new Error("Token is not for chat-api. Sign in again.");
  }

  return toUser(payload, token);
}

export function extractBearer(header) {
  if (!header || typeof header !== "string") {
    return null;
  }
  return header.startsWith("Bearer ") ? header.slice("Bearer ".length) : null;
}

function toUser(payload, accessToken) {
  const roles = payload.role == null ? [] : Array.isArray(payload.role) ? payload.role : [payload.role];
  return {
    id: payload.sub,
    name: payload.name ?? payload.preferred_username ?? payload.email ?? "Campus user",
    email: payload.email ?? "",
    roles,
    plan: payload.plan ?? "campus",
    tenantId: payload.tenant_id ?? "",
    accessToken,
  };
}

export function isStaff(user) {
  return user.roles.includes("Teacher") || user.roles.includes("Administrator");
}

export function isAdmin(user) {
  return user.roles.includes("Administrator");
}
