# Fibonacci estimate — User Registration & Login

## Estimate

**Recommended story estimate: 8 points** on the Fibonacci scale (`1, 2, 3, 5, 8, 13, ...`).

| Dimension | Fibonacci rating | Why |
| --- | ---: | --- |
| Complexity | 8 | The slice crosses browser UI, HTTP endpoints, validation, password hashing, cookie session handling, and persistence. |
| Effort | 8 | It needs both user-facing flows, safe error handling, a protected page, repository code, and acceptance tests. |
| Risk | 5 | Authentication has security-sensitive failure modes; the local JSON file also has concurrency and deployment limits. |
| Uncertainty | 5 | Framework details and security behavior need validation, while the task leaves storage and endpoint design to the implementer. |

Dimension ratings describe different aspects of the same story; they are **not added together**. The overall estimate is 8 because the feature is a bounded, single-purpose authentication slice with standard framework support, but still has meaningful security and integration work. A production-ready deployment, password reset, email verification, MFA, or multi-instance persistence would raise the estimate and is outside this exercise.

## Assumptions

- The app is a learning/demo implementation, not a production identity platform.
- One local JSON file is acceptable, and the app runs as a single instance.
- Registration and login use email plus password; there is no email verification or recovery flow.
- A same-origin HttpOnly cookie is acceptable as the session mechanism in place of a JavaScript-readable bearer token.
