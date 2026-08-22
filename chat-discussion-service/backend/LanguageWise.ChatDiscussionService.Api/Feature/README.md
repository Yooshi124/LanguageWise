# Feature code — Discussion / Chat Forum

Owner: **Lachlan**

Everything in this folder is Lachlan's feature work. It is kept **separate from the
skeleton** and is **not wired up yet**.

## What is in here

`Models/` holds the domain model classes for the discussion forum: `Post`, `Comment`,
`Like` and `Image`. They are plain C# classes carried over unchanged from the original
`ChatDiscussion/backend/Models/` folder, still in the `ChatDiscussion.Models` namespace.

They are compiled by the API project but nothing references them yet — there are no
tables, repositories or endpoints behind them.

## What the skeleton is, and why it is separate

The rest of this microservice is a working proof of concept built around a placeholder
`SampleItems` table. It exists to show the wiring end to end:

```
frontend (nginx)  ->  backend API  ->  database service  ->  SQLite
```

- `database/` owns the SQLite file, creates `SampleItems`, seeds 10 rows and exposes CRUD
- `backend/` calls the database service over HTTP and exposes `/api/sample-items`
- `frontend/` renders the rows

Use it as the reference for how to plumb a real feature through the three tiers.

## Wiring this feature up

Roughly, per model:

1. Add a table to `database/LanguageWise.ChatDiscussionService.Db/sql/schema.sql`.
2. Add a repository next to `Data/SampleItemRepository.cs`.
3. Map endpoints in the database service `Program.cs`.
4. Add a typed client in `backend/.../Clients/` and expose an API endpoint.
5. Render it in `frontend/index.html`.
6. Add NUnit tests in `tests/`.

`SampleItems` can stay as long as it is useful, and be deleted once the real feature
covers the same ground.

## Notes on the models as they stand

- `Post.Comments`, `Comment.Post`, `Like.Post`/`Like.Comment` and `Image.Post`/`Image.Comment`
  are object references. SQLite has no notion of these, so the repository layer will need
  to either join explicitly or load them separately.
- `UserId` is present on every model but there is no `User` model yet (it is commented out
  in each file). Something needs to own users before these become foreign keys.
- Reference-type properties are non-nullable and uninitialised, so the compiler warns unless
  they are set. Consider `required` or a nullable annotation when wiring them up.
