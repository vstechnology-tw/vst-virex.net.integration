# Atomic Start product context

`SystemStartRequest.ProductInfo` is optional. When omitted/null, Start uses the current product as before. When supplied, a receiver must accept the complete product context together with the work under its existing admission lock, copy the additional-field dictionary, and persist that accepted snapshot with each result. A rejected Start must not replace current product information.

`ProductInfo.AdditionalFields` and `ResultSummary.AdditionalFields` are optional generic string maps. Keys are application-defined and nonempty. Missing fields and fields containing an empty string are different; null values are invalid. `ProductInfo.Snapshot()` owns a read-only dictionary copy. Existing six field names retain their meaning.

REST Start and scoped Start use the same nested `productInfo` shape. MQTT `StartWithOptionsAsync` forwards it, and TCP callers use `SendStartWithOptionsAsync`. Existing overloads and omitted fields retain their prior wire shape. Extra fields do not redefine recipe/condition semantics, perform defect aggregation, or authorize a change of operation source.
