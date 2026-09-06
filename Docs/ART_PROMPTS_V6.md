# Luna v6 생성 기록

2026-09-06. 사용 도구/모드: 내장 image_gen, 참조 이미지 편집 → 배경 추출 편집. 별도 CLI나 래퍼를 사용하지 않았다.

최종 프로젝트 자산: `Assets/Resources/Art/Generated/luna-nocturne-gothic-v6.png`.
최종 생성 파일: `C:/Users/송인석/.codex/generated_images/01a07272-83c6-7f50-bc6e-055a0c2eeb7f/exec-5351470b-e294-4fa8-88ce-a38489b0d199.png`.
검증: 1024×1536 Format32bppArgb, 모서리 alpha 0. Unity 선택·지도·필드에 적용하여 투명 경계와 방향을 확인했다. 이안은 왼쪽, 루나는 오른쪽을 바라본다. 구체적인 나이는 설정하지 않았다.

## 1. 루나 재디자인

참조 순서: `Assets/Resources/Art/Generated/ian-character-portrait-v2.png`, `Assets/Resources/Art/Generated/luna-instrument-maker-v4.png`.
출력: `exec-b99c4b58-da59-4988-80da-225092b6b393.png` (위 생성 폴더). RGB에 체크무늬가 포함되어 투명 배경 수정이 필요했다.

```text
Use case: stylized-concept. Asset type: transparent full-body standing character illustration for GRAPHACLYSM. Redesign Luna, Ian's younger white-haired sister. Input 1 is Ian: use only rendering finesse and to establish opposite direction; Ian's camera and body face image-left. Input 2 is previous Luna: retain white hair, pale teal eyes, lunar astronomical instrument identity, but change hairstyle, clothing, proportions, mood, and standing direction. New Luna MUST face image-RIGHT in clear three-quarter view: nose, torso, feet, and held instrument point to the right, with long hair trailing to image-left. A distinctly younger, petite adolescent-looking face, soft round cheeks, short delicate chin, curious reserved expression; natural youthful proportions, not chibi. Very long silver-white softly waved hair to calves, partly tied with narrow black velvet ribbons, small antique crescent hairpiece, no bob haircut. Modest elegant Victorian Gothic fantasy dress: closed ivory lace high collar, long puff sleeves with fitted cuffs, midnight-plum velvet bodice, full layered ankle-length charcoal and muted-violet skirt with refined ivory lace hems, opaque stockings and low-heeled button boots. Small silver filigree moon and star embroidery, aged silver fasteners. No cape, no camera, no butterfly motifs, no trousers, no revealing outfit. Hold a compact delicate antique silver lunar armillary/drafting instrument at waist-to-chest height on image-right, hands anatomically clear. Full figure, grounded poised stance, both feet visible, entirely inside a tall 2:3 canvas. Nocturnal antique atmosphere expressed THROUGH the character's moonlit white hair, soft indigo-violet shadows and restrained silver highlights, not through a background. Premium refined painterly anime fantasy illustration, subtle cloth texture, expressive eyes, elegant silhouette and calm details. Actual transparent PNG with real alpha channel around figure and in gaps. Absolutely no painted checkerboard, no black/white/color background, no floor or halo rectangle, no text, no watermark. One character only. Preserve a clear independent sibling identity; do not recolor or mirror Ian's costume.
```

## 2. 배경 추출 1차

참조: 1차 출력 파일. 출력: `exec-c6f1462a-d375-4b36-a20f-d3ee428fde36.png`. RGB 배경이 남아 최종 자산으로 사용하지 않았다.

```text
Use case: background-extraction. Edit ONLY the background of the attached Luna illustration. Remove the baked-in white/gray checkerboard completely and return an actual transparent PNG with a genuine alpha channel. Preserve the exact character: young soft face looking image-right, very long silver-white hair flowing image-left, midnight-plum Victorian Gothic long dress, ivory lace high collar and long sleeves, silver lunar armillary in her hands, both boots, original proportions, colors, framing, pose and all fine lines. Keep fine hair strands, lace and openings in the instrument. No design changes, no mirroring, no new background or floor, no checkerboard painted behind her, no glow rectangle. Pixels outside the figure and in empty gaps must have real alpha transparency.
```

## 3. 배경 추출 2차 · 최종

참조: 2차 출력 파일. 출력: `exec-5351470b-e294-4fa8-88ce-a38489b0d199.png`. 실제 RGBA 투명 배경을 확인하고 위 프로젝트 경로에 복사했다. 편집 과정에서 생긴 세부 묘사 변화는 최종 그림과 게임 화면으로 검토했다.

```text
Background removal / transparent sprite export. Return this exact Luna character as an RGBA cutout with alpha=0 outside the character. The checkerboard in the input is an unwanted opaque image, not transparency. Erase it. Preserve the entire illustration and the face pointing RIGHT, all white hair flowing LEFT, dark Gothic dress, silver instrument and boots. Keep transparent empty gaps between hair strands and inside metal rings. Deliver genuine transparent pixels, NOT a visualization of transparency, NOT a new checkerboard, NOT any solid backdrop. Do not alter the character or pose. Actual transparent background.
```

