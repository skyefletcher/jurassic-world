# jurassic-world

A 3D dinosaur survival game built with Godot 4.7.2 .NET, C#, and .NET 10, adapted from [ice-age-world](https://github.com/skyefletcher/ice-age-world). All nine dinosaurs are playable from the beginning. There are no coins, purchases, or unlocks.

Open `project.godot` in **Godot 4.7.2 .NET** and press **F5**. The .NET 10 SDK must be installed. From a terminal:

```powershell
dotnet build
godot --path .
```

The loading screen stays up while the procedural world is generated. Choose Play alone, Host, or Join, then choose your dinosaur and sex. For Velociraptor, choose **Blue, Echo, Charlie, or Delta**. Blue uses the approved skeletal Ferocious Industries model with silver skin and blue markings. Echo, Delta and Charlie also use the approved skeleton with their own brown, dark-green and plain olive-green skins: Blue has a blue flank stripe, Echo has a brown hide without facial scars, Charlie has a plain olive-green hide, and Delta has a dark-green hide. The appearance is remembered locally and shared with friends.

You hatch small and grow over ten minutes while fed and watered. Explore, drink, eat plants or hunt according to your dinosaur's diet, rest, and complete survival challenges. Herds and packs accompany the appropriate dinosaurs. Velociraptor and Anchiornis can find a mate, build a nest as a grown female, and hatch a family. Eggs hatch every five minutes. Death lets you hatch again as any dinosaur.

| Control | Action |
|---|---|
| WASD / arrows | Move; mouse looks around; wheel zooms |
| Right mouse | Walk forward |
| Shift | Sprint; T. rex in deep water: dive |
| Space | Jump; Anchiornis: launch a glide or grab a nearby tree; underwater: swim up |
| Hold F | Basic attack |
| Hold Q | Normal attack |
| Hold G | Special attack; Triceratops starts a ten-second charge (Space cancels) |
| E / left click | Eat or drink |
| R | Carry or put down a carcass; Anchiornis can also put down fish or eggs |
| C / X | Sit / lie down |
| N | Build a nest and lay eggs when grown, female, and paired |
| Tab / B | Switch dinosaur / open dinosaur selection |
| V | Change sex |
| M | Show or hide map |
| Esc | Pause solo play; open menu in multiplayer |
| F11 | Fullscreen |

Attacks apply the following **HP per second while in reach**, with one attack active at a time. G takes priority over Q, then F. Attacking uses stamina. Tail attacks strike behind the dinosaur; bites, kicks, headbutts, horn jabs, and stomps strike ahead. Ankylosaurus's shake strikes around its body. Grabbing specials restrain wild prey; T. rex can lift smaller wild dinosaurs. PvP uses the same damage rates and knockback; the detailed grab/lift restraints currently apply to wildlife.

| Dinosaur | Health | F DPS | Q DPS | G DPS | Stamina / 10 |
|---|---:|---:|---:|---:|---:|
| Velociraptor | 1000 | 20 | 50 | 90 | 8 |
| Ankylosaurus | 1800 | 10 | 25 | 90 | 7 |
| Gallimimus | 350 | 10 | 15 | 40 | 9 |
| T. rex | 2000 | 20 | 45 | 100 | 8 |
| Stegosaurus | 1600 | 12 | 30 | 85 | 6 |
| Pachycephalosaurus | 900 | 12 | 28 | 70 | 7 |
| Triceratops | 1750 | 15 | 35 | 85 | 8 |
| Anchiornis | 300 | 8 | 20 | 50 | 8 |
| Dromaeosaurus | 500 | 15 | 28 | 65 | 9 |

The jump, land/water speed, agility, health, stamina and attack definitions from the supplied sheet live in `scripts/DinosaurCatalog.cs`. Water score 0 means very slow swimming (0.5 metres/second as an adult). Jump score 0 means no jump. Anchiornis can climb trees and glide, losing height after its initial launch.

The approximately 1.5 km world has fern meadows, mixed prehistoric forests, lakes, a deep coastal bay, hills, caves, and eight places to discover: Raptor Lookout, Dinosaur Graveyard, Sentinel Rock, Emerald Springs, Basalt Arch, Basalt Steps, Ancient Ginkgo, and Amber Falls. The graveyard contains dinosaur bones and the falls have flowing water and mist.

Host or join with up to 16 players on UDP port 7777. Friends on the same network use the host's local address; internet play needs the host's public address and UDP port forwarding. As in the original game, terrain is deterministic and player positions, appearances, poses and combat are networked; wildlife, families, plants, and nests are simulated separately on each computer.

The models include procedural scale relief, countershading, mottled skin, sculpted brows, outward-facing glossy eyes, jaw interiors, lower teeth, muscle contours, toe scutes, and hooked claws. Stegosaurus has angular bevelled plates; Triceratops has a scalloped frill with bone points and ridges; Pachycephalosaurus has a ring of skull knobs; Ankylosaurus has keeled armour and a lobed tail club. Anchiornis uses an original Blender model with blue-grey plumage, a swept golden crest, cream flight feathers with dark spots, feathered arms and hind legs, and a long feathered tail. Its articulated pivots share the bird controller for walking, wingbeats, gliding, resting and feeding. The editable model is in art/anchiornis/anchiornis-review.blend. Repeated body-part meshes are shared between animals to limit memory use. The artwork remains stylised.

Engine integration checks run the real models and combat at 30/60/120 updates per second:

```powershell
dotnet build
godot --headless --path . res://tests/GameChecks.tscn
```

For an actual localhost multiplayer check, start these in two terminals:

```powershell
godot --headless --path . res://tests/NetworkChecks.tscn -- --server
godot --headless --path . res://tests/NetworkChecks.tscn -- --client
```

`tests/Preview.tscn` renders a dinosaur gallery to `.runtime/dinosaurs.png`; add `-- --world` to capture the world instead, or `-- --closeup` for a detailed raptor view at `.runtime/raptor-detail.png`. Runtime review images and logs are ignored by Git.

## Raptor movement study

Open `tests/RaptorLab.tscn` in Godot and press **F6**. The imported Ferocious Industries raptor is isolated here for art and animation review; Blue in the main game now uses this skeletal model; Delta shares the approved skeletal model with a separate dark-green skin; Echo also uses the shared model with a brown skin; Charlie shares the approved skeletal model with a plain olive-green hide.

The scene automatically cycles idle, walk, run, and stop. Click the mode button for camera-relative WASD movement, hold Shift to run, right-drag to orbit, scroll to zoom, Space to pause, and R to reset. Stationary view keeps the raptor in place; the playback slider slows both travel and animation. Buttons preview bite, eat, drink, and roar. The one-metre floor grid helps reveal foot sliding.

Walk/run cadence uses approximate travel speeds measured from matching source root-motion clips. Transitions blend and movement eases in/out. This is a flat-ground review, without terrain IK, collision, combat, or full gameplay integration. Turning does not yet have dedicated animations.

The approved Blender source and reproducible export script live in `art/raptor/`; exported textures, skinning, facial shape keys, and thirteen clips are embedded in `assets/raptor/raptor.glb`. Run `art/raptor/export_raptor.py` in the approved Blender scene to re-export; its temporary animation tracks are removed afterwards. Author, source, and licence notes are in `art/raptor/SOURCE.md`.

Validate import and movement with:

```powershell
godot --headless --path . res://tests/RaptorLab.tscn -- --check
```

Use `-- --capture` with a graphical renderer to save `.runtime/raptor-lab.png`; add `--detail` for `.runtime/raptor-skin-detail.png`.

The current movement study uses the detailed reference-inspired blue/grey skin in `art/raptor/raptor-blue-detailed-review.blend`, with silver-grey colour and curved blue eye markings, a muted blue stripe with pale broken edges, and the source detailed scale normal map. The earlier colour pass remains in `raptor-blue-review.blend`. The approved original green version remains in `raptor-review.blend`. `refine_blue_material.py` creates the current skin and `create_blue_material.py` preserves the earlier colour recipe; both use charcoal claws using native Blender material nodes and UV baking. Geometry, the original neck, facial controls, and animation clips are preserved. Run the exporter from the desired Blender version to select the study appearance.

Blue is playable with blended movement, attacks, feeding, jumping, resting and death animations. Sitting uses a crouched pose from the source rest transition; swimming reuses the gait animation. Validate with `godot --headless --path . res://tests/RaptorChecks.tscn`. Add `--blue` to the world preview to capture Blue in the actual world.

Delta uses `assets/raptor/delta-body.png` as a material override on the approved shared skeleton and animations. Her editable Blender skin lives in `art/raptor/raptor-delta-review.blend` and is generated by `create_delta_material.py`. Blue retains her own silver-blue texture. Select Velociraptor / Delta in a freshly started game, or run `tests/RaptorLab.tscn -- --delta` to review her movement. `-- --delta --capture --detail` saves `.runtime/delta-skin-detail.png`.

Echo uses `assets/raptor/echo-body.png` on the shared animated model: warm brown/ochre skin, darker mottling, a pale underside and no facial scars. Her editable source is `art/raptor/raptor-echo-review.blend`, generated by `create_echo_material.py`. After restarting the game, select Velociraptor / Echo. For the movement study, use `tests/RaptorLab.tscn -- --echo`; `-- --echo --capture --detail` saves `.runtime/echo-skin-detail.png`. Blue and Delta retain separate skin textures.
