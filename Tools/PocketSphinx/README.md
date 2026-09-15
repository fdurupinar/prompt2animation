# Optional local speech alignment

In the Chat Manager Inspector, enable **Use Pocket Sphinx Alignment**. The LLM still generates the emotional AUs and utterance. After macOS speech synthesis, the recording and utterance are passed to local PocketSphinx; aligned phonemes are converted to visemes by PhonemeToVisemeConverter. No Gemini viseme request is made on this path. Playback is blocked until preparation completes.

Leave the toggle off to retain Gemini viseme generation. Existing Resources viseme files and batch recordings are not regenerated automatically.

## Installation (macOS / Unity Editor)

From the project root:

```sh
python3 -m venv Tools/PocketSphinx/.venv
Tools/PocketSphinx/.venv/bin/python3 -m pip install -r Tools/PocketSphinx/requirements.txt
```

The dependency is pinned to source revision `f20ff1b7a5db64c5e892a798479e20df86e79a35`, because the 5.1.1 release can fail with `state_align_search.c: Alignment failed in frame ...` on otherwise valid recordings. Source installation requires a working C/C++ build toolchain (on macOS, Xcode command-line tools).

If upgrading an environment that already has the 5.1.1 release, force replacement: the patched source reports the same package version, so an ordinary install may leave the release wheel in place.

```sh
Tools/PocketSphinx/.venv/bin/python3 -m pip install --force-reinstall --no-deps "pocketsphinx @ https://github.com/cmusphinx/pocketsphinx/archive/f20ff1b7a5db64c5e892a798479e20df86e79a35.zip"
```

A local environment has been installed for this workspace. The environment is not committed or bundled into a player build.

The default Python path is this project's `.venv/bin/python3`. For another interpreter, add a **Pocket Sphinx Aligner** component, set **Python Executable** to its absolute path, and assign that component to Chat Manager's **Pocket Sphinx Aligner** slot. Desktop builds require an explicitly configured Python runtime. This integration does not support WebGL or mobile sandbox execution. The existing speech synthesis path uses macOS `/usr/bin/say`.

**Pronunciation Overrides** optionally accepts a TextAsset containing an English-word-to-ARPAbet dictionary, for example:

```json
{"ew": "UW"}
```

Unknown words cause a visible error instead of guessed timestamps. Write digits in the transcript as spoken words or provide their pronunciation. Model alignment can still fail for difficult audio; there is no silent fallback to Gemini.

## Existing audio / another caller

`PocketSphinxAligner.Align(wavPath, transcript, onSuccess, onFailure)` is a coroutine and can be called independently of Chat Manager. On success, `Result.VisemeJson` contains the JSON accepted by `FACS.SetVisemeData`, and `Result.phonemes` contains the original aligned intervals. Calling this method alone does not alter FACS, save resources, or start playback.

Chat Manager publishes audio, AU response and visemes only after successful preparation and saves cues as `Assets/Resources/Visemes/<audio-basename>_visemes.json`. Alignment failures preserve previous playback data and show an error. The Python helper resamples an analysis copy to mono 16 kHz and adds 200 ms of analysis-only trailing silence. It verifies transcript word coverage, rejects missing pronunciations and invalid intervals, and excludes padding from its results. The C# converter validates the final phoneme intervals against measured audio duration before generating cues. Original audio is not modified.

These are English-model acoustic alignment estimates. They do not guarantee natural coarticulation or measured visible lip contact. Diphthong splitting and phoneme-to-viseme mapping remain procedural approximations.
