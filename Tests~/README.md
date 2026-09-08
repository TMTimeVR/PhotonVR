# Tests

These cover the fixes written up in [../reports/security-review.md](../reports/security-review.md).

They compile the real source files in this package against small stand-ins for the
Unity, PUN2 and TextMeshPro APIs, then check what the fixes were supposed to
guarantee. Room code space and alphabet, display name sanitisation, colour clamping,
room size clamping, room name prefixes, and the saved-cosmetics round trip.

```
dotnet run --project test.csproj
```

Needs the .NET SDK. Exits non-zero on any failure.

The folder name ends in `~` so Unity ignores it, and that part is not optional.
`Stubs.cs` declares types in the `UnityEngine` namespace. If Unity ever compiled
them they would collide with the real ones and break your project.

These are not Unity Test Framework tests and they do not run in the editor. They
cover logic you can check without a Photon connection or a scene. Anything that needs
a live room, you still have to test by hand.
