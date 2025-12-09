## System Architecture Overview

본 프로젝트는 **QR 코드 기반 위치 초기화 + ARCore + A* 경로 탐색 + 실내 공간 데이터 레이어**를 통합하여
실내 환경에서 **스마트폰 AR 내비게이션**을 제공하는 모바일 애플리케이션이다.

아래 구성요소는 모바일 Unity 애플리케이션과 실내 공간 데이터 레이어 사이의 상호작용을 중심으로 설계되었다.

---

##  System Components
<img width="6000" height="3375" alt="dlalwl3" src="https://github.com/user-attachments/assets/8a90ee51-bd5a-4c22-853b-7960b5870af4" />

### 📱 Unity Mobile Client

Unity 기반 모바일 애플리케이션으로 AR 실내 내비게이션의 핵심 로직을 실행한다.

| Module                     | Description                                                     |
| -------------------------- | --------------------------------------------------------------- |
| **QR Recognition Module**  | QR 코드를 인식하여 위치 초기화 및 Drift 보정 수행. QR 텍스트 데이터를 Node ID 및 Pos로 파싱 |
| **ARCore Module**          | 카메라/IMU 센서의 시각·관성 데이터를 받아 위치 추정 및 Tracking                      |
| **Camera Sensor**          | 10Hz 프레임 기반 시각 피드                                               |
| **IMU Sensor**             | 100Hz 관성 데이터 기반 자세 정보 제공                                        |
| **A* Pathfinding Module**  | 시작 Node(현재 위치)와 목표 Node를 입력받아 최단 경로 탐색                          |
| **AR Navigation Renderer** | 탐색된 경로를 3D AR 라인으로 시각화                                          |
| **UI/UX Module**           | 목적지 선택, 안내 메시지, 경로 업데이트 UI 제공                                   |

---

###  Indoor Spatial Data Layer

Unity 애플리케이션과 분리된 정적인 공간 정보화 계층.

| Component                      | 역할                          |
| ------------------------------ | --------------------------- |
| **BIM-based 3D Spatial Model** | 실제 건물 구조 기반의 3D 실내 모델       |
| **NavMesh**                    | 경로 탐색에 필요한 Spatial Graph 정보 |
| **QR Location Database**       | QR 코드별 위치/노드/좌표 매핑 정보       |

---

##  Data Flow Summary

| Input      | Processing             | Output            |
| ---------- | ---------------------- | ----------------- |
| 카메라/IMU 센서 | ARCore Tracking        | 실시간 pose 추정       |
| QR 코드 이미지  | QR Recognition         | Node ID + 초기 pose |
| Node ID    | A* Pathfinding         | 최적 경로 Path        |
| Path       | AR Navigation Renderer | 공간 내 시각화 경로       |

---

## 📌 Key Features

* QR 기반 위치 초기화로 **초기 위치 오류 최소화**
* ARCore VIO 기반 실시간 위치 추정
* BIM·NavMesh 기반 실내 경로 탐색
* 실내 지형을 반영한 **경로 안내 UX**
* **드리프트 누적 보정** (QR 재인식 시)

---

## 📁 Project File Structure (Suggested)

```
navigation-app/
 ├─ Assets/
 │   ├─ Scripts/
 │   │   ├─ QR/
 │   │   │   ├─ QRRecognition.cs
 │   │   │   ├─ QRCodeParser.cs
 │   │   ├─ AR/
 │   │   │   ├─ ARCoreTracker.cs
 │   │   │   ├─ ARNavigationRenderer.cs
 │   │   ├─ Pathfinding/
 │   │   │   ├─ AStar.cs
 │   │   │   ├─ GraphNode.cs
 │   │   ├─ UI/
 │   │   │   ├─ DropdownManager.cs
 │   │   │   ├─ NavigationHUD.cs
 │   ├─ Models/
 │   ├─ Prefabs/
 │   ├─ Materials/
 ├─ Resources/
 ├─ StreamingAssets/
 │   ├─ QRLocationDatabase.json
 ├─ Docs/
 │   ├─ architecture_diagram.png
 │   ├─ demo.gif
 │   ├─ BIM_to_Graph_Conversion.pdf
 ├─ README.md
```

---

##  Sensor & Module Characteristics

| Module         | Frequency | Notes                      |
| -------------- | --------- | -------------------------- |
| Camera Sensor  | 10 Hz     | Frame-based Tracking       |
| IMU Sensor     | 100 Hz    | Orientation + acceleration |
| ARCore         | Fusion    | Sensor preintegration      |
| QR Recognition | Event     | Node ID 기반 위치 보정           |
| A* Pathfinding | On-demand | 목적지 변경/업데이트 발생 시           |

---

##  Workflows

1. QR 코드 스캔 → 현재 위치 초기화
2. 목적지 선택 (UI Dropdown or 자동 설정)
3. A* 경로 탐색 → 노드 기반 Path 생성
4. ARCore Tracking 기반 실시간 Navigation
5. 경로 이탈 또는 Drift 감지 시 자동 경로 갱신
6. QR 재인식 시 오차 보정

---

##  Future Improvements

* UL-VIO / VIFT 기반 외부 VIO 통합
* 건물 층간 이동 지원 (엘리베이터 / 계단)
* 실내 군집 탐색 Crowd-aware 경로 계획
* BLE·UWB·Wi-Fi Fingerprinting 융합

