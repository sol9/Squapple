# prolly.works 홈페이지와 Squapple 개인정보처리방침 준비

- 확인일: 2026-10-13
- 첫 출시 방향: 무료, 광고 없음, IAP 없음. 광고 코드는 후속 도입을 위해 보관한다.
- 홈페이지 편집·배포는 사용자가 별도로 진행한다. 이 문서는 필요한 내용과 검토용 문구를 전달한다.
- 사용자가 확인한 연락처: 일반 문의 `hello@prolly.works`, 앱 지원 `support@prolly.works`, 개인정보 문의 `privacy@prolly.works`.

## 1. 현재 사이트에서 확인한 것

| 페이지 | 현재 상태 | 필요한 작업 |
| --- | --- | --- |
| [영문 홈](https://prolly.works/) / [한국어 홈](https://prolly.works/ko/) | 소개, 언어 전환, 이메일 연락처, 아직 공개 작업물 없음 | Squapple 소개와 지원·앱 방침 링크 추가. 스토어 링크는 실제 공개 후 연결 |
| [영문 웹사이트 방침](https://prolly.works/privacy/) / [한국어 웹사이트 방침](https://prolly.works/privacy/ko/) | 웹사이트만 적용 대상으로 명시. Cloudflare 호스팅, 이메일 문의, 테마의 로컬 저장 설명 | 아래 방문 분석 설명 불일치 수정 |
| Squapple 소개·지원·앱 방침 | 현재 확인한 홈·방침 페이지에서는 연결되지 않음 | 앱용 내용을 추가하고 앱·스토어에서 직접 연결 |

한국어·영어 방침 모두 방문 분석 스크립트가 없다고 적혀 있지만, 전달받은 공개 HTML에는 `static.cloudflareinsights.com/beacon.min.js`와 `data-cf-beacon`이 있다. 테마 스크립트는 `prollyworks-theme`를 브라우저 로컬 저장소에 저장한다. 분석 스크립트 포함 여부와 테마 저장은 서로 다른 처리다.

Cloudflare는 이 beacon을 Web Analytics 성능 지표 수집에 사용한다고 설명한다. 스크립트가 실제 사이트에 포함돼 있다는 점을 확인했으며 계정의 보관 설정이나 실제 수집 결과까지 확인한 것은 아니다. [Cloudflare 공식 설명](https://developers.cloudflare.com/web-analytics/data-metrics/data-origin-and-collection/)

분석을 유지하면 방침의 ‘방문 분석 스크립트 없음’을 삭제하고 목적·제공자·수집 지표를 실제 설정에 맞게 설명한다. 분석을 끄려면 자동 삽입 설정까지 끈 뒤 공개 HTML에서 beacon이 없어졌는지 확인한다. 쿠키를 쓰지 않는 분석이어도 분석 스크립트가 없는 것과 같지 않다.

## 2. 웹사이트 방침과 앱 방침의 범위

웹사이트 방침은 방문 요청·호스팅·방문 분석·브라우저 테마·이메일 문의를 설명한다. Squapple 방침은 앱의 로컬 저장·외부 데이터 전송·SDK·보관 및 삭제·앱 지원 문의를 설명한다.

반드시 URL을 둘로 나눠야 한다는 뜻은 아니다. 한 문서에서도 범위를 명확히 구분할 수 있다. 현재 웹사이트 방침이 앱 정보를 별도 제공한다고 안내하므로, Squapple 전용 페이지를 추가하는 방식이 관리하기 쉽다. 앱 방침에는 앱 이름을 명시하고 웹사이트 방침으로 연결해, 앱에서 외부 사이트를 열 때 적용되는 내용을 구분한다.

다음은 **신설할 경로 제안**이며 아직 게시된 URL이 아니다.

| 내용 | 영문 | 한국어 |
| --- | --- | --- |
| 앱 소개 | `/squapple/` | `/squapple/ko/` |
| 앱 지원 | `/squapple/support/` | `/squapple/support/ko/` |
| 앱 개인정보처리방침 | `/squapple/privacy/` | `/squapple/privacy/ko/` |

Google Play는 개인·민감 정보를 처리하지 않는 앱도 개인정보처리방침을 제출하고, 스토어와 앱 내에서 방침에 접근할 수 있게 요구한다. 앱 이름 또는 스토어의 개발자명이 방침에 식별돼야 하며 데이터 처리·문의·보관·삭제 내용이 실제 앱과 일치해야 한다. [Google Play 사용자 데이터 정책](https://support.google.com/googleplay/android-developer/answer/10144311?hl=en)

## 3. 앱 소개와 지원에 넣을 내용

### 앱 소개 문구

한국어:

> 사각사과 · Squapple은 사각형 안의 숫자 합을 10으로 맞춰 사과를 지우는 숫자 퍼즐입니다. 제한 시간 안에 더 많은 사과를 지우고, 같은 판을 다시 연습해 보세요. 로그인 없이 오프라인으로 플레이하며 기록은 기기에 저장됩니다. 첫 출시 버전은 무료이며 광고와 인앱 결제가 없습니다.

영어:

> Squapple is a number puzzle: select a rectangle whose numbers add up to 10 to clear its apples. Clear as many apples as you can before time runs out, or practise the same board again. Play offline without an account. Records are saved on your device. The initial release is free, with no ads or in-app purchases.

이는 첫 출시 계획에 맞춘 문구다. 실제 출시 후보 빌드에서 해당 조건을 확인한 뒤 게시한다. 출시 전에는 ‘출시 준비 중 / Coming soon’으로 표시하고, 스토어 버튼은 실제 링크가 생긴 뒤 추가한다. 리플레이·멀티·온라인 랭킹·클라우드 저장은 현재 제공 기능으로 소개하지 않는다.

### 지원 페이지

- 문의: `support@prolly.works`. 앱 버전·기기 모델·OS 버전·문제 발생 순서가 있으면 안내에 도움이 된다고 적는다. 비밀번호·결제 정보 등 불필요한 민감 정보는 보내지 않도록 안내한다.
- 규칙: 합이 10인 사각형을 선택하며 빈자리는 그대로 남는다. 기본 모드와 제한 시간을 설명한다.
- 기록: 최고 기록과 마지막 완료 판은 이 기기에 저장된다. 다른 기기와 직접 동기화하지 않는다.
- 같은 판 연습: 일반 최고 기록과 구분해 저장한다.
- 삭제: Android의 앱 데이터 삭제, iOS의 앱 삭제로 로컬 기록·설정을 제거할 수 있다. OS 백업이 있으면 복원될 수 있으며 백업 삭제는 플랫폼 설정에서 관리한다. iOS ‘앱 정리하기’는 앱 데이터 삭제와 다르다.
- 정책: 앱 개인정보처리방침과 웹사이트 개인정보처리방침 링크를 제공한다.

## 4. Squapple 앱 방침에 넣을 데이터 설명

| 항목 | 확인한 구현 / 계획 | 게시 전 확인 |
| --- | --- | --- |
| 계정·로그인 | 자체 계정·서버 없음 | 출시 빌드 유지 |
| 게임 기록 | 최고 점수, 규칙 버전, 보드 크기·제한 시간, 마지막 완료 판의 원본 보드·종료 이유·연습 구분을 PlayerPrefs로 기기 저장 | 최종 저장 형식, OS 백업·복원 설정 |
| 설정 | 소리·진동 설정 기기 저장 | 개인정보 민감 데이터를 이 저장소에 넣지 않음 |
| 광고·IAP | 첫 출시에서 제공하지 않음. 공통 설정 `Assets/Resources/AdsSettings.asset`의 `enableAds=false`이면 앱의 UMP 갱신·광고 초기화·요청·예고·빈도 쓰기를 실행하지 않음 | 네이티브 SDK·광고 식별자 권한 포함 및 자동 실행 여부 |
| 분석·진단 | 앱 코드에 자체 분석 요청 없음. Unity Analytics·클라우드 충돌 보고는 비활성 설정이나 `m_EngineDiagnosticsEnabled`는 1로 남아 있음 | Unity 엔진 진단 및 Cloud 연결 설정과 출시 빌드의 실제 전송 |
| 지원 이메일 | 이메일 주소·사용자가 보내는 문의 내용 | 메일 서비스 제공자, 실제 보관·삭제 기준, 연락처 수신 가능 여부 |
| 홈페이지 방문 | 브라우저로 외부 사이트를 방문하면 웹사이트 방침 적용 | 홈페이지 beacon과 방침 일치 |

**광고 화면을 껐다는 사실만으로 모든 SDK의 데이터 수집이 없다고 확정하지 않는다.** 현재 Git 패키지·네이티브 광고 플러그인·IAP 패키지는 프로젝트에 남아 있다. 광고 없는 출시 후보에서는 SDK 제외 또는 자동 실행·권한·전송 점검을 먼저 마치고 Google Play Data safety·App Store 개인정보 표시를 확정한다. SDK 제외는 연동 소스와 검증 기록을 삭제하는 작업과 별개다.

Unity 6의 엔진 진단 데이터는 별도로 설정·검토해야 한다. [Unity 공식 설명](https://docs.unity.com/en-us/cloud/developer-data-framework/built-for-unity-6) Apple도 포함된 외부 SDK의 데이터 처리를 앱 개인정보 답변에 반영하도록 안내한다. [Apple 공식 설명](https://developer.apple.com/app-store/app-privacy-details/)

## 5. 앱 개인정보처리방침 초안

**아래는 검토용 초안이다.** SDK·진단, 운영자 표기, 메일 처리와 OS 백업 점검 후 확정한다. `[확정 필요]`를 해결하기 전 전체 문서를 그대로 게시하지 않는다. 시행일은 실제 게시·적용일로 정한다.

### 한국어

**사각사과 · Squapple 개인정보처리방침**

시행일: `[실제 적용일]`

이 방침은 prolly.works가 제공하는 사각사과 · Squapple 앱에 적용됩니다. `[운영자·스토어 개발자 표기 확인]` 개인정보 문의는 `privacy@prolly.works`, 앱 지원은 `support@prolly.works`로 보내실 수 있습니다.

**앱 이용과 기기에 저장하는 정보**

Squapple은 회원가입이나 로그인 없이 플레이할 수 있는 오프라인 퍼즐입니다. 최고 점수, 보드 크기와 제한 시간 등 게임 조건, 마지막 완료 판의 보드와 결과, 소리·진동 설정을 기기에 저장합니다. 이 정보는 기록 표시, 같은 판 연습, 설정 유지에 사용되며 앱의 게임 코드가 개발자의 자체 서버에 전송하지 않습니다.

**광고, 결제 및 외부 서비스**

첫 출시 버전은 광고와 인앱 결제를 제공하지 않습니다. `[출시 빌드의 SDK·Unity 엔진 진단 설정과 데이터 처리 점검 결과를 여기에 반영. 광고 없음과 데이터 수집 없음은 구분.]`

**보관과 삭제**

로컬 게임 기록과 설정은 사용자가 앱 데이터를 삭제하거나 앱을 삭제할 때까지 기기에 보관됩니다. Android에서는 시스템 설정의 앱 데이터 삭제, iOS에서는 앱 삭제를 이용할 수 있습니다. iOS의 앱 정리 기능은 데이터를 남길 수 있습니다. 운영체제나 사용자가 설정한 백업에 데이터가 포함되는 경우 백업 보관·삭제·복원은 해당 플랫폼 설정과 정책을 따릅니다. 개발자는 사용자의 기기에 저장된 게임 기록을 직접 열람하거나 원격으로 삭제하지 않습니다.

**이메일 문의**

지원 또는 개인정보 문의 이메일을 보내면 이메일 주소와 메시지에 포함한 정보를 받습니다. 문의 확인과 답변에 필요한 범위에서 사용합니다. `[실제 메일 서비스 제공자 및 보관·삭제 기준 확정]` 문의 정보의 열람·정정·삭제를 요청하려면 `privacy@prolly.works`로 연락해 주세요.

**웹사이트와 방침 변경**

앱에서 외부 브라우저로 prolly.works를 방문하면 웹사이트의 개인정보처리방침이 적용됩니다. 앱 기능이나 데이터 처리 방식이 변경되면 이 방침과 필요한 스토어 안내를 갱신합니다.

### English

**Squapple Privacy Policy**

Effective date: `[Actual effective date]`

This policy applies to the Squapple app provided by prolly.works. `[Confirm the operator and store developer identity.]` Contact `privacy@prolly.works` for privacy enquiries and `support@prolly.works` for app support.

**Using the app and information stored on your device**

Squapple is an offline puzzle that does not require registration or an account. The app stores best scores, game settings such as board size and time limit, the board and result of the last completed round, and sound and vibration preferences on your device. This information supports records, practice on the same board, and saved preferences. The game's code does not send it to a server operated by the developer.

**Advertising, purchases and external services**

The initial release does not offer advertising or in-app purchases. `[Insert the verified data practices of SDKs and Unity engine diagnostics in the release build. An ad-free app is not automatically an app with no data collection.]`

**Retention and deletion**

Local records and preferences remain on your device until you delete the app's data or delete the app. Android provides an app data deletion option in system settings. On iOS, delete the app; offloading may retain its data. If operating system or user-configured backups include app data, retention, deletion and restoration of those backups follow the platform's settings and policies. The developer cannot directly view or remotely delete game records stored on your device.

**Email enquiries**

When you send a support or privacy enquiry, we receive your email address and the information in your message. We use it to review and respond to your enquiry. `[Confirm the email service provider and actual retention and deletion practices.]` To request access, correction or deletion of enquiry information, contact `privacy@prolly.works`.

**Website visits and policy changes**

If you visit prolly.works in an external browser, the website's privacy policy applies. We update this policy and relevant store disclosures when the app's features or data practices change.

## 6. 남은 준비 순서

1. 홈페이지의 분석 사용 여부를 정하고 웹사이트 방침의 불일치를 수정한다.
2. Squapple 소개·지원 페이지와 앱 방침 초안을 한국어·영어로 준비한다.
3. 광고 없는 출시 후보에서 네이티브 광고·IAP 의존성, Unity 진단 설정, 권한·외부 전송·OS 백업을 점검한다.
4. 점검 결과와 운영자·이메일 처리 정보를 반영해 앱 방침을 확정·게시한다.
5. 앱 설정의 일반 개인정보처리방침 링크와 스토어의 정책·지원 URL을 연결하고 데이터 표시를 맞춘다. 광고용 UMP 설정 버튼과 일반 앱 방침 링크는 서로 다른 용도다.

광고 재도입은 후속 업데이트에서 별도로 준비한다. 그때 운영 SDK·동의 메시지·데이터 표시·광고 포함 여부·방침을 함께 갱신한다.
