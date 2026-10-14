# 출시 빌드 설정 점검

- 최초 점검: 2026-10-13
- 마지막 확인: 2026-10-14 (한국 시간)

## 결론

현재 Android 프로필은 **Release APK**를 만드는 설정이다. IL2CPP·ARM64와 게임 씬 연결은 준비돼 있다. 대상 SDK는 설정 화면 오류를 우회해 API 36으로 저장했다. Play 제출에는 실제 산출물 검증, AAB, 업로드용 서명, 양쪽 앱 식별자 확정과 아이콘 준비가 남았다.

2026-10-14 Unity Pipeline으로 열린 에디터를 조회하여 `Development=False`, `BuildType=Release`를 재확인했다. 에디터에 있던 변경을 저장한 뒤, 후속 조사에서 SDK 메뉴 변환 오류를 확인하고 공식 설정 API로 대상 SDK 36을 적용했다. 식별자를 다시 변경하거나 새 빌드를 만들지는 않았다.

사용자는 Google Play Console에 APK/AAB를 아직 업로드하지 않았다고 확인했다. 현재 값과 권장 조치를 구분하며, **권장값으로의 정리는 아직 미완료**다.

## 실제 설정과 권장 조치

| 항목 | 확인한 현재 값 | 출시 전 조치 |
| --- | --- | --- |
| Unity | 6000.6.4f1 | 현재 버전 유지. 최종 산출물로 검증 |
| 활성 프로필 | `Assets/Settings/Build Profiles/Android™.asset` | 기기 테스트 APK와 Play 제출 AAB의 용도를 구분 |
| Development / Build Type | 꺼짐 / Release | 제출용은 이 상태 유지 |
| Build App Bundle | 꺼짐 | Play 제출용 프로필에서 켜기 |
| 프로파일러·스크립트 디버깅 | 꺼짐 | 유지 |
| 압축 | Lz4 | 출시 차단 문제 없음. Lz4HC는 용량·빌드 시간 비교 후 선택 가능 |
| 씬 | 전역 목록의 `Assets/Scenes/game.unity` 하나 | 유지 |
| 프로필 Player Settings 재정의 | 없음 | 현재 프로젝트 공통 설정을 사용함 |
| Company / Product | `prolly.works` / `Squapple` | 현재 표기 사용 가능 |
| Android 유효 식별자 | `works.prolly.Squapple` | 현재 사용자 설정을 보관. 소문자 `works.prolly.squapple` 권장안과 대소문자가 다르므로 첫 업로드 전 확정 |
| iOS 유효 식별자 | `com.prolly.works.Squapple` | 별도 명시 항목이 없어 자동 생성된 값이 조회됨. 첫 iOS 빌드 전 확정 |
| 식별자 재정의 | 켜짐 (`overrideDefaultApplicationIdentifier=1`), Android 항목 있음 | Android는 명시 설정됨. iOS 명시 설정은 남음 |
| 표시 버전 / Android 코드 | `0.1` / `1` | 첫 테스트 `0.1.0` / `1` 제안. 이후 업로드마다 Android 코드를 증가 |
| iOS 빌드 번호 | `0` | 첫 배포용 빌드는 `1`부터 관리 제안 |
| 스크립팅 / 아키텍처 | IL2CPP / ARM64 | 유지 |
| Android 최소 SDK | API 26 | 기존 검증 출발점 유지 |
| Android 대상 SDK | API 36 | Unity API와 저장 파일에서 36 확인. 메뉴 변환 오류에 유의하고 최종 산출물도 검증 |
| 코드 제거 | Minimal | 초기 출시는 유지. 용량 때문에 검증 없이 강화하지 않기 |
| 그래픽 API | Vulkan → OpenGLES3 | 현재 실기기 동작을 기준으로 유지. 다른 기기에서도 확인 |
| 화면 방향 | Auto Rotation, 정방향·거꾸로 세로 허용 | 세로 전용은 맞음. 정방향만 원하면 Portrait로 고정 |
| Android 창 크기 조절 | 켜짐 | 태블릿·최신 OS에서 실제 레이아웃 확인 필요 |
| 사용자 Gradle 템플릿 | Main·Properties·Settings 사용 | 해당 템플릿 파일은 이미 Git에 포함돼 있음. 최종 빌드로 의존성 검증 |
| 업로드용 서명 | Custom Keystore 꺼짐, 키 경로·별칭 없음 | 업로드 키 생성·별도 백업 후 제출용 서명 설정 |
| Android 앱 아이콘 | 지정된 아이콘 텍스처 없음 | 최종 앱 아이콘과 Adaptive Icon 준비 |
| iOS | 최소 15.0, 자동 서명 꺼짐 | Mac의 Xcode에서 팀·서명·실기기 경로 확인 |

현재 식별자는 문법 오류로 판단한 것이 아니다. 도메인 기반의 소문자 식별자를 명시적으로 고정하자는 제안이다. 식별자를 바꾸면 기존 테스트 설치와 별도 앱으로 취급되므로 로컬 기록도 자동으로 이어지지 않는다.

10월 13일에는 Android 식별자가 `com.prolly.works.Squapple`, 대상 SDK가 Auto였다. 10월 14일 커밋 `d912c47`에는 식별자 `works.prolly.Squapple`과 대상 SDK 30이 보관됐다. 이후 사용자가 화면에서 36을 선택했다고 알려 SDK 메뉴 변환 오류를 확인했고, 현재 작업 파일은 36으로 수정돼 있다. 최초 API 30 관측은 실제 저장값이었지만 사용자 선택 의도가 아니었다.

신규 앱과 업데이트는 2026-08-31부터 API 36 이상을 대상으로 해야 한다. 최소 지원 OS와 대상 API는 별개이며 최소 API 26을 유지하면서 대상 API 36을 사용할 수 있다. [Google Play 대상 API 요구사항](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en)

Development 옵션, APK/AAB 전환 및 압축 방식의 의미는 [Unity 6.6 Android 빌드 설정](https://docs.unity3d.com/6000.6/Documentation/Manual/android-build-settings.html)을 기준으로 확인했다.

## 빌드 경로에서 주의할 점

### Target API 메뉴와 저장값 불일치 — 2026-10-14

현재 설치된 Unity `6000.6.4f1`의 열린 `PlayerSettingsEditorExtension` 객체에서 읽기 전용 리플렉션으로 직접 재현했다.

| 메뉴 문구 | `ConvertAndroidTargetSDKIndexToVersion` 반환값 |
| --- | --- |
| Android 12L / API 32 | 26 |
| Android 15 / API 35 | 29 |
| Android 16 / API 36 | 30 |
| Android 17 / API 37 | 31 |

메뉴 목록은 API 32부터 시작하지만 변환 객체의 `k_MinAndroidTargetAPILevel`은 26이었다. 선택 위치를 실제 API로 환산할 때 6이 작아지는 불일치다. `AndroidSdkVersions.AndroidApiLevel36` 열거형 자체의 정숫값은 36이므로 YAML의 30이 API 36을 뜻하는 정상 저장 방식은 아니다. 프로젝트 코드·패키지 C# 검색에서 대상 SDK를 30으로 덮어쓰는 코드는 찾지 못했다.

아래 공식 설정 API를 Unity Pipeline으로 실행하고 `AssetDatabase.SaveAssets()`로 저장했다.

```csharp
UnityEditor.PlayerSettings.Android.targetSdkVersion = UnityEditor.AndroidSdkVersions.AndroidApiLevel36;
UnityEditor.AssetDatabase.SaveAssets();
```

적용 직후 Unity API는 `AndroidApiLevel36`, 정숫값 36을 반환했고 `ProjectSettings.asset`의 `AndroidTargetSdkVersion`도 36으로 저장됐다. 후속 조회 시 Pipeline 연결이 없어 에디터 재접속·재시작 후 유지 여부와 실제 APK/AAB 값은 아직 검증하지 못했다. 저장 파일은 후속 읽기에서도 36이다. 이 세션에서 재현한 에디터 문제이며 다른 설치 전체에 대한 재현이나 공식 수정 버전은 확인하지 않았다. 드롭다운에서 다시 선택한 뒤에는 실제 저장값을 확인한다.

### 개발용 빌드 메뉴와 기존 APK

`Tools → Squapple → Build Android Development` 메뉴는 별도의 개발용 도구다. 현재 프로필의 Release 설정과 관계없이 코드에서 Development APK와 임시 식별자 `com.squapple.development`를 지정한다. Play 제출에는 이 메뉴를 사용하지 않는다.

이 도구는 빌드 후 이전 식별자 문자열을 복구하지만, 식별자 자동 생성 여부까지 별도로 보관하지 않는다. 출시 식별자를 고정할 때 이 임시 전환이 필요한지 함께 정리하고 빌드 전후 설정 보존을 확인한다.

확인한 기존 파일 `Builds/Android/Squapple-development.apk`의 수정 시각은 **2026-10-11 15:58:39**다. 10월 13일 광고 비활성 변경을 검증하는 산출물로 사용할 수 없다. 새 산출물의 경로·시각·버전·식별자를 확인한 뒤 설치한다.

## 광고 없는 첫 출시의 추가 확인

- `Assets/Resources/AdsSettings.asset`: `Enable Ads` 꺼짐, `Use Test Ads` 켜짐, `Initialize Ads In Editor` 꺼짐. 앱 코드에서 광고·동의 초기화와 광고 안내를 건너뛰는 설정이다.
- Google Mobile Ads와 External Dependency Manager 패키지는 그대로 있다. 광고 플래그는 SDK와 네이티브 권한을 빌드에서 제거하는 스위치가 아니다.
- Unity Purchasing 패키지는 현재 manifest에 없으며 서비스 설정도 비활성이다.
- Unity 서비스·Analytics·Cloud Diagnostics Reporting 설정은 비활성이다. 그러나 `InsightsSettings.m_EngineDiagnosticsEnabled=1`이고 활성 빌드 프로필의 Engine Diagnostics는 `Project Settings`를 따른다. 이 파일만으로 실제 데이터 송신 여부를 확정하지 않는다. 출시 후보에서는 진단 사용 여부를 명시하고 실제 포함 설정과 동작을 확인한다.
- 최종 Android 산출물의 병합 Manifest, 광고 ID 관련 권한, 네이티브 SDK 초기화와 진단 설정을 확인한 뒤 앱 개인정보처리방침과 스토어 데이터 보안 답변을 확정한다.
- 네이티브 라이브러리를 포함하므로 최종 산출물의 16 KB 페이지 크기 호환성을 확인한다. Unity 버전만으로 포함된 모든 SDK의 호환성을 보장하지 않는다. [Android 16 KB 페이지 크기 지원](https://developer.android.com/guide/practices/page-sizes)

## 다음 작업 순서와 완료 기준

1. **앱 식별자 확정 및 빌드 용도 정리**: 식별자를 양쪽 플랫폼에 명시하고 테스트 APK·제출 AAB 프로필을 구분한다. 대상 SDK 36은 적용했으며 에디터 재접속·최종 산출물에서도 유지되는지 확인한다.
2. **광고 없는 출시 후보 점검**: 진단 설정·SDK·권한을 확인하고 최신 APK로 오프라인 플레이, 재실행, 저장, 광고 미노출을 검증한다. 완료 기준은 실제 산출물과 정책 설명이 일치하는 것이다.
3. **배포 자료와 서명 준비**: 업로드 키, 앱 아이콘, 앱 전용 개인정보처리방침, 스토어 문구·스크린샷을 준비한다. 홈페이지 수정은 사용자가 별도로 진행하며 [홈페이지·개인정보처리방침 전달 문서](website-and-privacy-handoff.md)를 사용한다.
4. **Play 내부 테스트용 AAB**: 제출용 빌드를 검증하고 Play 내부 테스트 경로로 설치·업데이트를 확인한다. 계정별 후속 테스트 요건은 Play Console에서 확인한다.
5. **iOS 실기기 경로 확보**: Mac mini M1에서 Xcode·서명을 준비하고 iPhone의 Safe Area, 백그라운드 복귀, 저장을 확인한다. Android 확인만으로 양쪽 검증을 완료 처리하지 않는다.

## 키스토어 관리 결정 — 2026-10-14

- 사용자가 키스토어를 직접 별도 관리하기로 했다. 빌드 전 자동 주입과 외부 보관소 연동은 현재 범위에서 제외한다.
- 키스토어는 프로젝트 밖의 고정 위치에 보관하고 별도 복구용 사본을 유지한다. 파일과 비밀번호는 Git에 넣지 않는다.
- `.gitignore`에 `.jks`·`.keystore` 및 해당 Unity meta 제외 규칙을 추가한다. 이 규칙은 실수로 새 파일을 추가하는 것을 막으며 백업을 대신하지 않는다.
- Unity의 Android Publishing Settings에서 파일·키 별칭을 연결하고 빌드 시 필요한 비밀번호를 입력한다.
- 현재 프로젝트에는 Custom Keystore가 비활성이고 키 경로·별칭도 비어 있다. 키 생성·백업·Unity 연결은 완료로 표시하지 않는다.

## 이번 보관 범위와 확인 결과 — 2026-10-14

- Android Release APK 프로필과 meta, 개발 APK 빌드 메뉴와 meta를 버전 관리에 포함한다.
- 회사 표기, 버전, Android 식별자·대상 SDK, 세로 방향·시스템 바·텍스처·Gradle 템플릿 관련 현재 Player Settings 변경을 보관한다.
- URP 에셋의 셰이더 사전 필터 값과 전역 런타임 설정 목록 변경을 현재 에디터 상태로 함께 보관한다. 별도의 렌더링 최적화나 화면 검증을 수행했다는 뜻은 아니다.
- Unity Pipeline 설정 조회 성공. 컴파일 확인은 `up_to_date`, 오류 0개·경고 0개였다. 이번에는 게임 로직을 바꾸지 않아 게임 테스트를 재실행하지 않았다.
- 새 APK/AAB 생성, 기기 설치, 스토어 업로드 및 서명 키 생성은 수행하지 않았다.
