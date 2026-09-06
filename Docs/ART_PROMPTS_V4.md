# 루나 v4 생성 기록

2026-09-06. imagegen 스킬의 built-in image_gen 모드를 사용했다. 사용자 요청은 이안의 색상 교체처럼 보이는 루나를 독립된 디자인으로 바꾸는 것이다. 흰머리 동생이라는 관계는 유지한다.

최종 저장: `Assets/Resources/Art/Generated/luna-instrument-maker-v4.png` — 1024×1536, 실제 RGBA. 게임 타이틀·선택·지도·전장 표식에 연결했다. 이안 원본과 이전 루나 PNG는 보존했다.

## 1. 참고 이미지를 사용한 재디자인

모드: built-in image_gen, referenced_image_paths로 이전 루나 초상을 그림의 렌더링 참고에만 사용. 새로운 단발·바지·천문 작도륜·컴퍼스·포즈를 요청했다.

전체 프롬프트:

```text
Use case: stylized-concept. Asset type: full-body transparent character illustration for the original mathematical spellcraft game GRAPHACLYSM. Redesign LUNA, the white-haired younger sister of a long-black-haired caped photographer named Ian, so she has a genuinely independent silhouette and profession. Reference image is ONLY a reference for painterly anime rendering finesse and youthful adult face; replace the hairstyle, outfit, props, pose and silhouette completely. Luna is a young adult celestial instrument maker: short luminous white chin-length bob with one slender braid, small asymmetrical crescent-metal hairpin, thoughtful lively pale teal eyes. Outfit: fitted high-collar ivory shirt, cropped asymmetrical dark teal waistcoat jacket with a single long narrow translucent mint back panel, high-waisted broad tapered charcoal trousers tucked into ivory ankle boots, fine champagne brass fasteners and a slim instrument belt. No skirt, no cape, no butterfly motif, no handbag, no camera. Her signature instrument is a delicate large transparent astronomical drafting wheel, held vertically beside her torso; concentric mechanical gears with a few fine luminous rose/teal mathematical curves, fingers visibly grasping a brass drawing compass over the wheel. A lively three-quarter standing pose, weight on one leg, other foot slightly forward; clear face and both hands; hands anatomically clean, not extra fingers. Restrained ivory, charcoal, desaturated sea-glass teal, small muted peach lining. Refined premium fantasy rhythm-game illustration, airy pale light, meticulous layered cloth and antique brass texture, subtle painted shadows and crisp expressive anime face. Modern tailoring mixed with antique observatory craft, wistful but curious expression. Full figure including boots, no cut-off extremities, leave modest transparent margin, tall 2:3 composition. Actual transparent RGBA background, no backdrop, no floor, no glow rectangle, no text, no logos, no watermark. This must look like an independently designed sibling, never a white recolor of a long-haired girl in a cape and layered skirt.
```

첫 출력은 캐릭터 디자인을 채택했지만 체크무늬 배경이 RGB에 들어 있었다. 이를 최종 투명 자산으로 쓰지 않았다.

## 2. 실제 투명 배경 추출

모드: built-in image_gen, 첫 출력 이미지를 referenced_image_paths로 편집. 외형을 유지하며 배경만 추출했다.

```text
Use case: background-extraction. Edit target: the attached Luna illustration. Remove ONLY the white-and-gray checkerboard which is currently baked into the RGB image. Deliver an actual transparent PNG with a real alpha channel, empty transparent pixels outside the figure and within open gaps. Preserve the exact character drawing, short white bob, teal and ivory cropped jacket, dark trousers, boots, mechanical transparent drafting wheel, brass compass, face, hands, colors, pose, full-body framing, and all fine details. Do not redraw or redesign her. Keep the drafting wheel's fine brass lines and translucent glass, and the thin cloth panel. No checkerboard image painted behind her, no white backdrop, no dark backdrop, no glow rectangle, no added shapes. Transparency must be actual alpha, not a simulated checkerboard pattern.
```

최종 원본: `C:/Users/송인석/.codex/generated_images/01a07272-83c6-7f50-bc6e-055a0c2eeb7f/exec-15dba291-b78a-4208-af1a-579e5a6ad2d2.png`.

.NET 이미지 검사에서 Format32bppArgb와 모서리 alpha 0을 확인했다. 실제 Unity 선택 화면과 필드 표식은 `Logs/RoguelikeV4Captures/02-luna-redesign.png`, `22-calculator-720p.png`에 있다. 별도의 Python 이미지 편집은 하지 않았다.

천문 도구 조율자라는 직업 이미지는 이번 외형 제안이며 구체적인 나이·과거 서사를 확정하지 않는다. 전체 아트 조사 출처와 UI 규칙은 `ART_DIRECTION.md`에 있다.
