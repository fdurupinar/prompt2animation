# CC4 Viseme-AU Confounding: Quick Reference

## Top 10 Critical Confoundings (Ranked)

### 🔴 CRITICAL (Must Fix)
1. **AH/AE + AU27 (Jaw Open)** 
   - Both drive jaw rotation independently
   - No coordination → unnatural jaw movement
   - Timing mismatches create discontinuities

2. **Wide Vowels (AE, AH) + AU18 (Pucker)**
   - Suppresses vowels → "ah" becomes "a_"
   - Speaking "yeah" with contempt emotion breaks phoneme

3. **Wide Vowels (AE, AH) + AU22 (Funnel)**
   - Explicitly suppressed in code
   - Forces choice: emotion OR clear speech

### 🟠 HIGH (Should Fix)
4. **AE + AU23 (Mouth Tighten)**
   - Suppresses vowel wideness
   - "Say" sounds pinched under tension

5. **EE Vowel + AU20 (Mouth Stretch)**
   - Both widen mouth → double effect
   - Clownish/unnatural appearance
   - No suppression mechanism

6. **B_M_P + AU22 (Funnel)**
   - Bilabials need closed mouth
   - Funnel tries to purse/constrict
   - Over-constrained mouth shape

7. **F_V Fricative + AU18 (Pucker)**
   - Puckering disrupts air flow path
   - "fuh" → distorted articulation
   - No suppression (gap in code)

8. **S_Z Fricative + AU27 (Large Jaw Open)**
   - Fricatives need precise mouth closure
   - Large jaw opening breaks articulation
   - No suppression (gap in code)

### 🟡 MEDIUM (Nice to Fix)
9. **Fricatives (S_Z, F_V) + AU23 (Tighten)**
   - Over-tightening disturbs exact shape needed
   - Results in buzzing/distorted sound

10. **AU12 (Smile) + Wide Vowels (AH)**
   - Smile pulls mouth corners back
   - Reduces effective mouth opening / intelligibility
   - Subtle but persistent effect

---

## Confounding Matrix (Simplified)

```
VISEMES →      B_M_P  F_V   TH    S_Z   R    AH   AE   EE   IH   OH   W_OO CH_J
                 ║     ║     ║     ║     ║    ║    ║    ║    ║    ║     ║    ║
ACTION UNITS ↓   ║     ║     ║     ║     ║    ║    ║    ║    ║    ║     ║    ║
AU12 (Smile)     -     -     -     -     -    ✓    ✓    ✓    -    ✓     ✓    ✓
AU15 (Frown)     -     -     -     -     -    -    -    ✓    -    -     -    -
AU18 (Pucker)    -     ✓     -     ✓     -    ✓    ✓    -    -    ✓     ✓    ✓
AU20 (Stretch)   -     -     -     -     -    -    ✓    ✓    -    ✓     -    -
AU22 (Funnel)    ✓     ✓     -     -     -    ✓    ✓    -    -    ✓     ✓    ✓
AU23 (Tighten)   -     ✓     -     ✓     -    ✓    ✓    -    -    -     -    -
AU24 (Press)     -     ✓     ✓     ✓     -    ✓    ✓    -    -    -     -    ✓
AU26/AU27        ✓     ✓     ✓     ✓     ✓    ✓    ✓    -    ✓    ✓     ✓    ✓
(Jaw Open)       

Legend:
✓ = Conflict exists
- = No major conflict
⚠ = Suppression exists (handling implemented)
```

---

## Why These Happen

### Root Cause 1: Articulatory vs. Emotional Semantics
**Visemes** describe HOW lips/jaw move to produce sound (articulatory).
**AUs** describe emotional states (facial expression).
→ These systems evolved independently; semantics often clash.

### Root Cause 2: Blendshape Reuse
Same shape (e.g., `JAW_OPEN`) used for:
- Phonetic effect (open vowels)
- Emotional effect (surprise)
- No priority system → conflicts

### Root Cause 3: Suppression Gap
Current code uses `FixConfoundingKeys()` with hardcoded suppression rules.
**Coverage**: ~60% of conflicts
**Gap**: Some AU-viseme pairs not addressed
→ Unpredictable behavior when gaps triggered

### Root Cause 4: Sequential Application
1. Visemes computed
2. AUs applied (overwrites/suppresses viseme values)
3. No blending or priority merging
→ Last-writer-wins: fragile

---

## Manifestation Examples

### Example 1: "Ah!" with Surprise
**Expected**: Open-mouth "Ah" + surprise brow raise + eye wide
**What Happens**:
```
Frame T+0.0s: "Ah" viseme → jaw opens 10°
Frame T+0.1s: AU5 (eye wide) applied → adds eye wideness
Frame T+0.2s: AU27 (mouth stretch) applied → jaw rotation value replaced
Result: Jaw snaps → discontinuity visible
        Mouth opening inconsistent across frames
        Looks like glitch, not emotion
```

### Example 2: "Hi" (EE) + Sadness (AU22 Funnel)
**Expected**: Bright "Hi" smile vowel + sad mouth funnel (expression)
**What Happens**:
```
EE weight = 0.8
AU22 applied → MOUTH_PUCKER_UP_L, MOUTH_PUCKER_UP_R activated
ApplySuppression(aeConfounders, 1 - 0.8) applied
  → aeConfounders list doesn't include AU22-specific shapes
Result: EE viseme proceeds normally
        AU22 and EE viseme both active
        Mouth = wide (EE) + funneled (AU22) = visual contradiction
        "Hi" sounds happy but face looks sad (incongruent)
```

### Example 3: "Yes" with Anger (AU23)
**Expected**: Sharp "Yes" with tight angry expression
**What Happens**:
```
S_Z viseme needs precision mouth shape
AU23 (tighten) applied
Suppression check: AU23 not in FixConfoundingKeys()
Result: Fricative + tension → over-constrained
        "Yes" → "Yess" or distorted hissing
        Intelligibility reduced
```

---

## Current Suppression Rules (Implemented)

```csharp
// From FixConfoundingKeys() in FACS.cs

if (wBMP > 0.05f) {  // Bilabial pronounced
    Suppress: MOUTH_SHRUG_UPPER, MOUTH_FUNNEL_*, MOUTH_DOWN_LOWER_*, MOUTH_FROWN_*
}

if (wEE > 0.05f) {   // "E" sound
    Suppress: MOUTH_SHRUG_LOWER
}

if (wAE or wAH > 0.05f) {  // Wide vowels "A" or "uh"
    Suppress: MOUTH_PUCKER_UP_*, MOUTH_FUNNEL_UP_*, MOUTH_TIGHTEN_*
}

if (wFV > 0.05f) {   // F/V fricative
    Suppress: MOUTH_FUNNEL_DOWN_*, MOUTH_SHRUG_LOWER, MOUTH_DOWN_LOWER_*
}
```

---

## Missing Suppressions (Gaps)

```csharp
// These should be added:

if (wAE > 0.05f || wAH > 0.05f) {  // Wide vowels
    Suppress for: AU20 (stretch), AU15 (frown - emotional conflict)
}

if (wFV > 0.05f || wSZ > 0.05f) {  // Fricatives
    Suppress for: AU18 (pucker), AU27 (large jaw open)
}

if (wSZ > 0.05f) {  // S_Z fricative
    Suppress for: AU27 (jaw open too wide)
}

if (wBMP > 0.05f) {  // Bilabials
    Suppress for: AU22 (funnel - over-constrains)
}

// Jaw coordination (broader issue)
// Don't just set jaw rotation; blend/accumulate with AU drives
```

---

## Severity Scale

| Icon | Level | Impact | User Notice |
|------|-------|--------|-------------|
| 🔴 | CRITICAL | Speech unintelligible OR jarring discontinuity | Immediately obvious |
| 🟠 | HIGH | Clear but distorted speech OR visible emotion-phoneme conflict | Usually noticed |
| 🟡 | MEDIUM | Subtle articulation/expression degradation | Noticed on repeat viewing |
| 🟢 | LOW | Minor visual/audio artifacts | Not noticed by most |

---

## Design Takeaway

**Problem**: Two independent animation systems (viseme + AU) were grafted together without:
1. ✗ Unified weighting/blending mechanism
2. ✗ Comprehensive conflict mapping
3. ✗ Priority system
4. ✗ Temporal coordination

**Solution Path**:
1. Map ALL viseme-AU pairs → identify conflicts
2. Classify conflicts (articulatory vs emotional vs timing)
3. Design resolution rules (suppress one system, blend, or alter UI)
4. Implement unified animation controller
5. Test edge cases (rapid speech + emotion changes)

---

## Files to Reference

- [CONFOUNDING_ANALYSIS.md](./CONFOUNDING_ANALYSIS.md) - Full detailed analysis
- [Assets/Scripts/Face/FACS.cs](./Assets/Scripts/Face/FACS.cs) - Line 715: `FixConfoundingKeys()`
- [Assets/Editor/CC4LipSyncEditor.cs](./Assets/Editor/CC4LipSyncEditor.cs) - Viseme mapping

---

**Last Updated**: Analysis of prompt2animation codebase
**Scope**: CC4 character + OVRLipSync visemes + FACS Action Units
