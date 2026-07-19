# SobekCM — Claude Context

## Project overview

SobekCM is an open-source digital library management system. This repository is a migration from **.NET 4.7.2 ASP.NET WebForms** to **.NET 10 ASP.NET Core**. Work is ongoing on the `UpgradeNet10` branch.

The main web application entry point is the `SobekCM` project. Core domain models live in `SobekCM_Core`. Rendering/HTML subwriters live in `SobekCM_Library`.

---

## Session storage — deliberate design split

All session keys are constants in `SobekCM_Core/MemoryMgmt/SessionCache_Keys.cs`. Never use raw string literals for session keys.

| What | Where | How |
|---|---|---|
| Primitive/string values | `ISession` | `context.Session.SetString(key, value)` / `GetString(key)` |
| Complex objects (workflow state, etc.) | `SessionObjectStore` | `context.SessionObject()[key] = obj` |
| `User_Object` | `ISession` via Protobuf | `CachedDataManager_UserCacheServices` (see below) |

**Why the split:** `ISession` (string-based) participates in ASP.NET Core's distributed session store and expires naturally. `SessionObjectStore` is a static in-process `ConcurrentDictionary` — useful for complex objects but has no expiration (known memory leak; deferred for future work).

**`User_Object` specifically uses `ISession`** because it is stored for every logged-in user, making the `SessionObjectStore` memory leak unacceptable for it.

---

## User_Object session helper

`SobekCM_Core/MemoryMgmt/CachedDataManager_UserCacheServices.cs` — static class, two methods:

```csharp
string CachedDataManager_UserCacheServices.UserToString(User_Object user)
User_Object CachedDataManager_UserCacheServices.StringToUser(string value)
```

Serializes/deserializes via Protobuf (Base64). `User_Object` is `[ProtoContract]`-attributed.

Files that read/write the user session key (`SessionCache_Keys.User`):
- `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` — read, write×2, clear
- `SobekCM_Library/MySobekViewer/Logon_MySobekViewer.cs` — write
- `SobekCM_Library/MySobekViewer/Preferences_MySobekViewer.cs` — null check×2, write×2
- `SobekCM_Library/MySobekViewer/OpenNJ_Register_MySobekViewer.cs` — null check×3, write×2
- `SobekCM_Library/MySobekViewer/NewPassword_MySobekViewer.cs` — null check
- `SobekCM_Library/HTML/MySobek_HtmlSubwriter.cs` — clear on logout

---

## SessionObjectStore — known issues

`SobekCM_Library/UI/SessionObjectStore.cs` — `ClearSession(sessionId)` is defined but never called anywhere. Sessions accumulate in memory until the app pool recycles. Replacing with `IMemoryCache` (sliding expiration) is the right long-term fix; deferred pending a discussion about the parallel `SobekCM_Application` state store.

---

## Migration conventions

- `Session.Add("key", value)` / `Session["key"]` (WebForms) → `Session.SetString` / `SessionObject()` per the table above
- `Response.Redirect` → `context.Response.Redirect` or `UrlWriterHelper.Redirect`
- `HttpContext.Current` → injected `HttpContext` passed through the request pipeline
- `Global.asax Session_End` does not exist in ASP.NET Core — no session expiry event fires
