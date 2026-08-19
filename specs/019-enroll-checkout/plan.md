# Implementation Plan: Enroll and checkout

**Spec**: `specs/019-enroll-checkout/spec.md`  
**Story**: CH-S19  
**Status**: Implemented

## Code to apply

| Area | Path |
|---|---|
| UI | `enrollment-mfe/src/app/checkout.ts`, `my-enrollments.ts`, catalog `course-detail.ts` |
| Enrollment | `EnrollmentEndpoints.cs`, sagas |
| Payment | `PaymentEndpoints.cs` (internal `X-Internal-Key` only) |
