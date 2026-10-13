# Cart and offer checkout integrity, 2026-10-13

Two legacy storefront routes could consume cart items or mark an offer processed before a sale was fully saved. A retry could create another order. This change uses the existing EcommerceSalesCreationLedger; it adds no database tables or migrations.

## Request and transaction contract

The existing cart GET and offer POST routes require a nonempty UUID Idempotency-Key. Deploy the matching storefront changes together: VAT cart sends withVat=1 and offer sends addCartItems=0/1. Older callers without a key receive400 before writes. Workplace principals use their supported invoice path and cannot use these root-client-only routes.

The immutable receipt binds the operation, authenticated client, VAT/offer mode and inbound request. Cart identity is a per-attempt client key, never the reusable cart UUID. The storefront retains the request and key in sessionStorage across failures and tab reload. A confirmed sale acknowledgment clears it; an explicitly marked rolled-back validation rejection permits correction. Generic errors and ambiguous outcomes keep the key.

All order, item assignment, status, number, availability, reservation, offer-processing and receipt writes use one open connection and ambient transaction. Replays return the saved sale even after the cart is empty or refilled. The persisted cart agreement is retained. Offer consumption verifies owner, kind, validity and active agreement; trusted offer rows supply prices, VAT and specification. Quantity must be finite and positive. The existing unreserved backorder behavior is preserved when availability rows are absent.

Cart mutation, expiry and all ecommerce sale creation paths take the same transaction-owned application lock before ledger/product locks. Conditional item moves and cart ownership checks reject stale edits after checkout. This is a correctness-first global lock and serializes these operations between users; throughput must be measured before replacing it with finer-grained locking. Other application writers do not participate in this ecommerce lock, so this is not a claim of system-wide stock concurrency closure.

Notifications and search invalidation start after commit and do not inherit the completed transaction. Notification delivery remains best-effort and can be lost on process failure; the permanent creation receipt is independent of delivery.

## Verification scope

The added mandatory disposable-SQL lane exercises the actual cart and offer services with real SQL transactions, application locks, ledger and conditional item moves. Repository test doubles issue fixture SQL for sale/stock/reservation writes and inject late reload failures. These tests establish transaction/replay behavior, not full production schema compatibility or six-report parity. Additional cases cover concurrent replay, different keys consuming the same cart, refilled-cart replay, stale item assignment, ownership and backorders.

Controller, validation, notification timing and storefront request/retry tests complement that lane. Validation:374 unit-suite cases passed;71 final focused cases passed after fixture fixes;16 mandatory SQL cases passed with zero skips;20 storefront tests and the production build passed. Unit-suite SQL methods can return early when no connection is set, so only the separate mandatory SQL run is accepted as SQL evidence. The first SQL failure logs are retained: fixture callbacks missed an optional repository parameter, the expected stale-item exception needed updating, and older ledger tests reopened pooled-disabled connections inside an ambient transaction. The fixtures now keep one enlisted connection and the final SQL run passes.

The separate live API suite was aborted after failures because it targets the independently deployed DEV application, not this candidate. All write-test opt-ins were unset. No live API acceptance is claimed. Retained logs, including failures, are in /private/gba-atomic-checkout-20261013.

## Report migration boundaries

This closes an ecommerce write-integrity gap identified during BUG1274 audit. It does not activate the native report readers, establish the final 1C baseline, prove all six reports update after native create/edit/cancel, or authorize deleting retained financial tables. Offer keeps OrderSource.Offer; the separate GBA manager-warehouse guard currently scopes Shop/unknown orders and requires a follow-up decision for offers. Source sync, business database writes and deployment were not performed by this change.

The separate agent's EUR/USD and large-account work remains independent. A read-only check at backend a510664c9 passed24 existing rate-resolution/history tests, including division by EUR-to-USD for USD-to-EUR conversion. It does not independently verify that agent's database observations or seven post-deployment queries.
