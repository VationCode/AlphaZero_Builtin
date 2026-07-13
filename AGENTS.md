# Project Architecture & Development Guidelines

## 1. 문서 목적

본 문서는 프로젝트의 아키텍처, 코드 책임, 폴더 배치 기준을 통일하기 위한 협업 가이드이다.

새로운 코드를 작성하거나 기존 코드를 수정할 때는 단순히 클래스의 형태나 이름을 기준으로 분류하지 않는다.

가장 먼저 다음을 판단한다.

> 이 코드의 소유 주체는 누구인가?

그 이후 해당 코드가 담당하는 역할을 기준으로 위치와 책임을 결정한다.

본 아키텍처의 목적은 많은 계층과 패턴을 강제하는 것이 아니다.

> 코드의 위치와 책임을 예측할 수 있고, 수정 시 영향 범위를 쉽게 파악할 수 있는 구조를 만든다.

---

# 2. 핵심 설계 원칙

## 2.1 소유 주체를 먼저 판단한다

클래스의 형태보다 해당 코드가 누구에게 귀속되는지를 먼저 판단한다.

예시:

```text
Player의 행동인가?
Enemy의 행동인가?
특정 Entity에 귀속되지 않는 게임 규칙인가?
게임 규칙과 관계없는 범용 기능인가?
외부 데이터를 다루는가?
객체 간 의존성을 조립하는가?
```

`MonoBehaviour`, `Manager`, `Controller`, `DTO` 등의 이름만으로 폴더를 결정하지 않는다.

---

## 2.2 Entity 내부 기능은 최대한 Entity가 소유한다

Player의 행동은 Player가 담당한다.

Enemy의 행동은 Enemy가 담당한다.

외부 객체와 협력한다는 이유만으로 System으로 분리하지 않는다.

예를 들어 Player가 Item을 습득하는 과정에서 Item Database와 InventoryModule을 사용하더라도 행동의 주체가 Player라면 Player Flow에 위치한다.

```text
Player PickupFlow
        ↓
Item Database 조회
        ↓
InventoryModule.AddItem()
```

---

## 2.3 System은 필요한 경우에만 분리한다

System은 모든 기능 실행을 담당하는 계층이 아니다.

Entity 내부에서 자연스럽게 해결되는 기능은 LEA 내부에서 처리한다.

다음과 같은 경우 System 분리를 고려한다.

- 특정 Entity에 귀속시키기 어려운 경우
- 여러 Entity가 동일한 게임 규칙을 사용하는 경우
- Entity의 행동보다 게임 규칙 자체가 중심인 경우

System은 필수 계층이 아니다.

---

## 2.4 구조를 맞추기 위한 코드를 만들지 않는다

빈 폴더 또는 의미 없는 클래스를 미리 생성하지 않는다.

LEA가 다음 구조를 가진다고 해서 모든 Entity가 반드시 모든 영역을 가져야 하는 것은 아니다.

```text
CompositionRoot
Domain
Flow
Module
```

필요한 책임이 발생했을 때 분리한다.

---

# 3. 전체 프로젝트 구조

```text
Scripts
│
├─ Installer
│
├─ Common
│
├─ LDA
│  ├─ Loader
│  └─ Database
│
├─ System
│  ├─ Inventory
│  ├─ Combat
│  ├─ Trade
│  └─ Spawn
│
└─ Entity
   ├─ Player
   │  ├─ LEA
   │  │  ├─ CompositionRoot
   │  │  ├─ Domain
   │  │  ├─ Flow
   │  │  └─ Module
   │  └─ View
   │
   ├─ Enemy
   │  ├─ LEA
   │  └─ View
   │
   ├─ NPC
   │  ├─ LEA
   │  └─ View
   │
   └─ Item
      ├─ LEA
      └─ View
```

최상위 영역은 다음 책임을 가진다.

| 영역 | 역할 |
|---|---|
| Installer | 상위 범위 객체 조립 및 의존성 연결 |
| Common | 게임 규칙을 모르는 범용 기반 코드 |
| LDA | 외부 데이터 로드 및 데이터 저장·조회 구조 |
| System | 특정 Entity에 귀속되지 않는 공용 게임 규칙 및 서비스 |
| Entity | 독립적인 상태와 역할을 가지는 게임 주체 |

---

# 4. Entity

Entity는 게임에서 독립적인 상태와 역할을 가지는 주체를 의미한다.

예시:

```text
Player
Enemy
NPC
Item
```

Entity는 자신의 상태와 행동을 최대한 스스로 구성한다.

```text
Entity
├─ LEA
└─ View
```

복잡한 내부 로직이 필요한 Entity는 LEA 구조를 적용한다.

단순 데이터 또는 월드 표현만 가진 Entity라면 LEA 구조를 강제하지 않는다.

---

# 5. LEA

LEA는 Entity 내부의 조립, 상태, 판단, 기능을 역할별로 분리하기 위한 Local Entity Architecture이다.

```text
LEA
├─ CompositionRoot
├─ Domain
├─ Flow
└─ Module
```

LEA의 역할은 다음과 같이 정의한다.

```text
CompositionRoot = 내부 전체 조립 / 외부 연결 / Entity Core
Domain          = 상태와 개념
Flow            = 판단과 행동 흐름
Module          = Entity 내부 기능 실행
```

핵심 원칙은 다음과 같다.

> CompositionRoot가 연결하고  
> Flow가 판단하며  
> Module이 실행하고  
> Domain이 상태를 가진다.

---

## 5.1 CompositionRoot

CompositionRoot는 Entity 내부의 전체 구조를 조립하는 로컬 Composition Root이다.

또한 외부에서 해당 Entity와 연결할 수 있는 Core 역할을 담당한다.

예시:

```text
PlayerCore
CameraCore
EnemyCore
```

주요 책임:

- Domain, Flow, Module 참조 구성
- Entity 내부 의존성 연결
- 내부 객체 Bind
- 외부 의존성 수신 및 연결
- Entity의 Core 진입점 제공
- Entity 내부 주요 객체 접근점 제공
- 필요한 초기화 순서 구성

예시 구조:

```text
외부
 ↓
PlayerCore
 ↓
Player LEA
├─ Domain
├─ Flow
└─ Module
```

예시:

```csharp
public class PlayerCore : MonoBehaviour
{
    public PlayerContext Context { get; private set; }

    public StateMachine StateMachine { get; private set; }
    public LocomotionModule LocoModule { get; private set; }
    public CombatModule CombatModule { get; private set; }

    public CameraCore CameraCore { get; private set; }

    public void Bind(CameraCore p_cameraCore)
    {
        CameraCore = p_cameraCore;
    }
}
```

CompositionRoot는 객체를 연결한다.

다음 책임을 과도하게 담당하지 않는다.

```text
게임 행동 판단 X
상태 전이 판단 X
세부 기능 실행 X
복잡한 게임 규칙 처리 X
```

구분:

```text
CompositionRoot
= 연결과 조립

Flow
= 판단과 흐름

Module
= 기능 실행
```

---

## 5.2 Domain

Domain은 Entity의 상태와 핵심 개념을 표현한다.

다음 질문을 담당한다.

> 이 Entity가 무엇을 가지고 있는가?

> 현재 어떤 상태인가?

예시:

```text
PlayerContext
PlayerState
HealthData
ItemDataDTO
WeaponDataDTO
```

Domain에는 다음 코드가 포함될 수 있다.

- 상태 데이터
- 데이터 모델
- 값 객체
- Enum
- Context
- Entity의 핵심 개념

Domain은 가능한 한 행동 흐름을 직접 제어하지 않는다.

---

## 5.3 Flow

Flow는 Entity의 행동 흐름과 판단을 담당한다.

다음 질문을 담당한다.

> 언제 무엇을 실행할 것인가?

> 현재 상황에서 다음 행동은 무엇인가?

예시:

```text
GroundMoveState
JumpState
CombatFlow
PickupFlow
StateMachine
```

주요 책임:

- 상태 판단
- 상태 전이
- 실행 시점 결정
- 입력에 따른 행동 결정
- 이벤트에 따른 행동 결정
- 기능 실행 순서 제어

예시:

```text
입력 확인
   ↓
현재 상태 판단
   ↓
이동 가능 여부 판단
   ↓
LocomotionModule 실행
```

Flow는 Module 또는 System을 사용할 수 있다.

```text
Flow
 ↓
Module
```

또는:

```text
Flow
 ↓
System
 ↓
Module
```

외부 객체를 사용하는 것 자체는 Flow의 책임을 벗어난 것이 아니다.

항상 행동의 주체를 기준으로 판단한다.

예를 들어 Player가 Item을 줍는 행동은 외부 Item과 Inventory를 사용하더라도 Player Flow에 위치할 수 있다.

---

## 5.4 Module

Module은 Entity가 보유한 기능 실행 단위이다.

다음 질문을 담당한다.

> 이 Entity가 자신의 기능을 어떻게 수행하는가?

예시:

```text
LocomotionModule
CombatModule
InventoryModule
EquipmentModule
```

예시 기능:

```csharp
LocomotionModule.Move();
CombatModule.Attack();
InventoryModule.AddItem();
InventoryModule.RemoveItem();
```

Module의 핵심 기준:

> 내 기능을 내가 수행한다.

예를 들어 Player가 보유한 InventoryModule은 자신의 Inventory 기능을 수행한다.

```text
Player
└─ InventoryModule
   ├─ AddItem
   ├─ RemoveItem
   └─ FindSlot
```

Module은 다른 Entity의 전체 행동 흐름이나 게임 전체 규칙을 과도하게 제어하지 않는다.

하나의 Module이 여러 Module의 실행 순서를 판단하기 시작한다면 Flow 책임인지 검토한다.

---

# 6. View

View는 Entity 또는 기능을 Unity Scene과 사용자에게 표현하는 역할을 담당한다.

View는 LEA 외부에 위치한다.

```text
Entity
├─ LEA
└─ View
```

구분:

```text
LEA
= 내부 조립, 상태, 판단, 기능

View
= 외부 표현
```

View에는 다음 코드가 포함될 수 있다.

- UI
- Animation 표현
- Effect 표현
- 월드 오브젝트 표현
- Scene 표현 객체

예시:

```text
PlayerAnimationView
PlayerEffectView
InventoryView
InventorySlotView
PickupItem
```

예를 들어 `PickupItem`은 Item 습득 흐름을 담당하지 않는다.

```csharp
public class PickupItem : MonoBehaviour
{
    public int ItemId;
}
```

이 객체는 월드에 존재하는 Item을 표현한다.

```text
Item/View/PickupItem
= 월드 Item 표현

Player/LEA/Flow/PickupFlow
= Player의 Item 습득 행동
```

---

# 7. System

System은 특정 Entity에 귀속되지 않는 공용 게임 규칙 또는 서비스를 담당한다.

```text
System
├─ Inventory
├─ Combat
├─ Trade
└─ Spawn
```

System은 단순히 여러 객체와 협력하는 코드를 의미하지 않는다.

Flow 또한 여러 외부 객체와 협력할 수 있다.

System의 핵심 판단 기준은 다음과 같다.

> 이 규칙을 특정 Entity의 책임으로 보는 것이 자연스러운가?

특정 Entity의 책임이라면 LEA에서 처리한다.

특정 Entity에 귀속되지 않고 게임 규칙 자체가 중심이라면 System으로 분리한다.

---

## 7.1 Inventory System 예시

Player와 NPC가 각각 InventoryModule을 가질 수 있다.

```text
Player
└─ InventoryModule

NPC
└─ InventoryModule
```

각 InventoryModule은 자신의 Inventory 기능을 수행한다.

```text
AddItem
RemoveItem
FindSlot
```

하지만 Inventory A에서 Inventory B로 Item을 이동하는 규칙은 어느 하나의 Entity 소유라고 보기 어렵다.

```text
Player Inventory
        ↓
      Item 이동
        ↓
Chest Inventory
```

이 경우 다음과 같이 분리할 수 있다.

```text
System
└─ Inventory
   └─ InventoryTransferSystem
```

```text
InventoryTransferSystem
        ↓
From InventoryModule.RemoveItem()
        ↓
To InventoryModule.AddItem()
```

---

## 7.2 Combat System 예시

Player가 공격을 결정하는 것은 Player Flow의 책임이다.

```text
Player CombatFlow
```

Enemy가 공격을 결정하는 것은 Enemy Flow의 책임이다.

```text
Enemy CombatFlow
```

하지만 공격자와 피격자 사이에서 공통으로 사용되는 Damage 규칙은 System으로 분리할 수 있다.

```text
System
└─ Combat
   └─ DamageSystem
```

구분:

```text
Player가 공격한다.
→ Player CombatFlow

Enemy가 공격한다.
→ Enemy CombatFlow

Damage를 계산하고 적용한다.
→ DamageSystem
```

---

## 7.3 System은 필수 계층이 아니다

다음 구조로 자연스럽게 해결된다면 System을 만들 필요가 없다.

```text
Flow
 ↓
Module
```

다음 상황이 발생할 때 System 분리를 검토한다.

```text
Player Flow에 동일 규칙
Enemy Flow에 동일 규칙
NPC Flow에도 동일 규칙
```

또는 다음 질문에 특정 Entity를 답하기 어려운 경우이다.

> 이 게임 규칙의 소유자는 누구인가?

System은 Entity가 할 수 없는 기능을 담당하는 영역이 아니다.

> Entity에 귀속시키는 것이 부자연스러운 게임 규칙을 분리하는 영역이다.

---

# 8. Common

Common은 특정 Entity뿐 아니라 특정 게임 기능에도 종속되지 않는 범용 기반 코드를 관리한다.

```text
Common
├─ Base
├─ Utility
├─ Extension
└─ Interface
```

예시:

```text
ObjectPool
ListExtension
MathUtility
Generic Base
```

Common의 코드는 가능한 한 프로젝트의 구체적인 게임 규칙을 알지 않아야 한다.

구분:

```text
Common
= 게임 규칙을 모르는 범용 코드

System
= 게임 규칙을 아는 공용 코드
```

예를 들어 `ListExtension`은 Inventory가 없어도 존재할 수 있다.

따라서 Common이다.

반면 `InventoryTransferSystem`은 Inventory라는 게임 개념이 없다면 의미가 없다.

따라서 System이다.

Common을 단순한 공용 코드 보관소로 사용하지 않는다.

---

# 9. LDA

LDA는 Layered Data Architecture이다.

LDA는 외부 데이터를 어떻게 가져오고 저장하며 탐색하는지를 담당한다.

```text
LDA
├─ Loader
└─ Database
```

LDA는 게임의 Domain 데이터 자체를 의미하지 않는다.

---

## 9.1 Loader

Loader는 외부 데이터를 읽는 방법을 담당한다.

예시:

```text
IDataLoader
JsonDataLoader
```

데이터 출처 예시:

```text
JSON
File
Server
Addressables
```

Loader는 데이터 로딩 방식과 출처에 집중한다.

---

## 9.2 Database

Database는 로드된 데이터를 저장하고 조회하는 기능을 담당한다.

예시:

```text
Database<TKey, TValue>
WeaponDatabase
ArmorDatabase
ItemDatabaseRegistry
```

예시 기능:

```text
Add
Get
TryGet
Contains
GetAll
Clear
```

---

## 9.3 DTO 위치 기준

DTO라는 이유만으로 LDA 폴더에 배치하지 않는다.

DTO가 특정 Entity 또는 Domain의 개념을 표현한다면 해당 Entity의 Domain이 소유한다.

예시:

```text
ItemDataDTO
WeaponDataDTO
ArmorDataDTO
```

해당 데이터는 Item이 무엇인지 표현한다.

따라서 Item Domain에 위치할 수 있다.

```text
Entity
└─ Item
   └─ LEA
      └─ Domain
         ├─ ItemDataDTO
         ├─ WeaponDataDTO
         └─ ArmorDataDTO
```

구분:

```text
ItemDataDTO
= Item이 무엇인가?

JsonDataLoader
= 데이터를 어떻게 읽는가?

WeaponDatabase
= 데이터를 어떻게 저장하고 조회하는가?
```

---

# 10. Installer

Installer는 상위 범위에서 객체 생성과 의존성 조립을 담당한다.

```text
Installer
├─ GameInstaller
├─ PlayerInstaller
└─ SceneInstaller
```

주요 책임:

- Entity 간 참조 연결
- System과 Entity 연결
- 외부 의존성 전달
- Bind 호출
- 상위 초기화 순서 구성
- Scene 또는 Game 범위 Composition Root 역할

예시:

```csharp
playerCore.Bind(cameraCore);
playerCore.Bind(inventorySystem);
inventoryView.Bind(playerInventoryModule);
```

Installer는 게임 기능을 직접 수행하거나 행동을 판단하지 않는다.

```text
판단 X
게임 기능 실행 X
상위 의존성 조립 O
```

객체가 직접 `FindObjectOfType` 또는 전역 검색을 반복하는 대신 Installer에서 명시적으로 관계를 구성하는 것을 우선한다.

---

# 11. Installer와 CompositionRoot의 차이

Installer와 LEA CompositionRoot는 모두 객체를 연결하지만 소유 범위가 다르다.

```text
Installer
= 상위 범위 조립

CompositionRoot
= 하나의 Entity 내부 조립
```

예시:

```text
GameInstaller
    ↓
PlayerCore ← CameraCore
    ↓
Player 내부 LEA 조립
```

`GameInstaller`는 Player와 Camera를 연결한다.

`PlayerCore`는 Player 내부의 Flow, Module, Domain을 연결한다.

구분:

```text
Installer
= Entity 간 연결 / System 연결 / Scene 조립

CompositionRoot
= Entity 내부 연결 / 외부 의존성 수신 / Entity Core
```

---

# 12. Manager

Manager는 독립적인 아키텍처 계층으로 정의하지 않는다.

Manager는 특정 영역 내부에서 대상을 관리하는 객체 역할이다.

> Manager는 어디에 위치하는지가 아니라 무엇을 관리하는지가 중요하다.

예를 들어 UI 전체를 등록하고 조회하며 활성 상태를 관리하는 객체가 있을 수 있다.

```text
System
└─ UI
   └─ UIManager
```

UIManager 역할 예시:

```text
Register
Open
Close
AllClose
```

반면 Player 내부 Buff 객체들을 관리하는 객체라면 Player LEA 내부에 위치할 수 있다.

```text
Entity
└─ Player
   └─ LEA
      └─ Module
         └─ BuffManager
```

구분:

```text
게임 공용 영역의 관리
→ System 내부

Entity 내부 대상의 관리
→ 해당 Entity LEA 내부
```

Manager는 아키텍처 계층이 아니다.

```text
Manager
= 아키텍처 계층 X

Manager
= 특정 대상을 등록, 조회, 생명주기 관리하는 객체 역할 O
```

최상위 `Manager` 폴더는 사용하지 않는다.

---

# 13. 기본 실행 흐름

## 13.1 Entity 내부 기능

```text
Input / Event
      ↓
Flow
      ↓
Module
      ↓
Domain 상태 변경
      ↓
View 반영
```

예시:

```text
Move Input
    ↓
GroundMoveState
    ↓
LocomotionModule
    ↓
Player 상태 및 Transform 변경
    ↓
Animation View 반영
```

---

## 13.2 System을 사용하는 기능

```text
Input / Event
      ↓
Entity Flow
      ↓
System
      ↓
각 Entity Module
      ↓
Domain 상태 변경
```

예시:

```text
Transfer 요청
      ↓
Player InventoryFlow
      ↓
InventoryTransferSystem
      ↓
From InventoryModule
To InventoryModule
```

System이 존재한다고 해서 모든 기능이 System을 통과할 필요는 없다.

---

# 14. 새로운 클래스 분류 기준

새로운 클래스를 작성할 때 다음 순서로 판단한다.

## 14.1 특정 Entity의 상태 또는 행동인가?

```text
Yes
→ Entity
```

이후 LEA 역할을 판단한다.

```text
내부 전체 조립 / 외부 연결 / Core
→ CompositionRoot

상태와 개념
→ Domain

판단과 흐름
→ Flow

기능 실행
→ Module

외부 표현
→ View
```

---

## 14.2 특정 Entity에 귀속되지 않는 게임 규칙인가?

```text
Yes
→ System
```

예시:

```text
DamageSystem
InventoryTransferSystem
TradeSystem
SpawnSystem
```

---

## 14.3 게임 규칙과 관계없는 범용 코드인가?

```text
Yes
→ Common
```

예시:

```text
ObjectPool
Extension
Utility
Generic Base
```

---

## 14.4 외부 데이터의 로드 또는 저장·조회와 관련되는가?

```text
Yes
→ LDA
```

---

## 14.5 상위 범위 객체의 의존성을 연결하는가?

```text
Yes
→ Installer
```

---

# 15. 협업 가이드라인

## 15.1 책임을 이름으로 표현한다

가능하면 클래스 이름만 보고 역할을 추측할 수 있도록 작성한다.

권장:

```text
InventoryTransferSystem
PlayerCombatFlow
LocomotionModule
InventoryView
JsonDataLoader
PlayerCore
```

책임이 불명확한 이름은 주의한다.

```text
GameManager
PlayerController
SystemManager
DataManager
MainController
```

Manager 또는 Controller라는 이름이 잘못된 것은 아니다.

단, 이름만으로 책임을 설명할 수 없다면 역할 분리를 검토한다.

---

## 15.2 CompositionRoot에 게임 로직을 넣지 않는다

Core 객체가 모든 기능을 직접 처리하지 않는다.

잘못된 방향:

```text
PlayerCore
├─ Move 계산
├─ Attack 판정
├─ Inventory 추가
├─ State 전이
└─ Animation 처리
```

권장 방향:

```text
PlayerCore
├─ Flow 연결
├─ Module 연결
├─ Domain 연결
└─ 외부 의존성 연결
```

실제 판단은 Flow가 담당하고 기능 실행은 Module이 담당한다.

---

## 15.3 Entity Flow에 게임 전체 책임을 넣지 않는다

Player Flow는 Player의 행동을 담당한다.

```text
PlayerCombatFlow
```

가 Enemy 전체 행동, Spawn, UI, Save까지 제어하지 않는다.

Flow의 기준은 행동의 주체이다.

---

## 15.4 Module 간 직접 결합을 과도하게 만들지 않는다

Module은 자신의 기능 수행에 집중한다.

```text
LocomotionModule
CombatModule
InventoryModule
```

하나의 Module이 다른 Module 전체의 흐름을 관리하기 시작한다면 Flow 책임인지 확인한다.

---

## 15.5 System을 만능 서비스로 만들지 않는다

모든 기능을 System으로 이동하지 않는다.

```text
Player가 이동한다.
→ Player LEA

Player가 점프한다.
→ Player LEA

Player가 Item을 줍는다.
→ Player LEA
```

System 분리는 소유 주체가 불명확한 게임 규칙이 있을 때 고려한다.

---

## 15.6 Common을 공용 코드 창고로 사용하지 않는다

두 곳 이상에서 사용된다는 이유만으로 Common으로 이동하지 않는다.

먼저 특정 게임 개념에 귀속되는지 확인한다.

```text
InventoryTransferSystem
→ Inventory 게임 규칙
→ System

DamageSystem
→ Combat 게임 규칙
→ System
```

Common에는 게임 Domain과 독립적인 범용 코드만 배치한다.

---

## 15.7 Manager 폴더를 만들지 않는다

Manager는 역할 이름이지 최상위 아키텍처 영역이 아니다.

Manager 클래스는 실제 소유 영역 내부에 배치한다.

```text
UIManager
→ System/UI

BuffManager
→ Entity/Player/LEA/Module
```

---

## 15.8 구조보다 책임을 우선한다

아키텍처 구조에 맞추기 위해 클래스를 억지로 분리하지 않는다.

하나의 작은 Entity가 단순한 Domain과 View만 필요하다면 다음 구조도 허용한다.

```text
Item
├─ Domain
└─ View
```

불필요하게 다음 구조를 강제하지 않는다.

```text
Item
└─ LEA
   ├─ CompositionRoot
   ├─ Domain
   ├─ Flow
   └─ Module
```

필요한 책임이 실제로 발생했을 때 구조를 추가한다.

---

# 16. Codex 작업 지침

Codex 또는 자동화 도구가 프로젝트 코드를 수정할 때 다음 규칙을 따른다.

1. 코드 수정 전에 관련 Entity, System, LDA 구조를 먼저 확인한다.
2. 클래스의 현재 위치만 보고 책임을 판단하지 않는다.
3. 코드의 실제 소유 주체와 책임을 분석한다.
4. Entity의 행동은 가능한 한 해당 Entity LEA 내부에 유지한다.
5. 외부 객체와 협력한다는 이유만으로 System으로 이동하지 않는다.
6. System은 특정 Entity에 귀속되지 않는 게임 규칙일 때만 사용한다.
7. Common에는 게임 규칙을 아는 코드를 배치하지 않는다.
8. DTO는 이름만 보고 LDA로 이동하지 않는다. Domain 소유 여부를 먼저 확인한다.
9. CompositionRoot는 내부 조립과 외부 연결에 집중한다.
10. CompositionRoot에 세부 게임 로직을 추가하지 않는다.
11. Installer는 Entity 간, System 간, Scene 범위 의존성 조립에 사용한다.
12. `FindObjectOfType` 등의 런타임 전역 탐색보다 명시적인 Bind와 의존성 연결을 우선한다.
13. Manager를 독립적인 아키텍처 계층으로 만들지 않는다.
14. 빈 아키텍처 폴더 또는 의미 없는 Base 클래스를 미리 만들지 않는다.
15. 기존 구조를 대규모로 변경하기 전 변경 이유와 영향 범위를 먼저 설명한다.
16. 요청이 분석만을 요구하는 경우 사용자의 명시적 지시 없이 코드를 수정하지 않는다.
17. 리팩터링 시 현재 게임 동작을 유지하는 것을 우선한다.
18. Unity Inspector 직렬화 참조, Prefab, Scene 참조 파손 가능성을 고려한다.
19. 클래스 또는 파일 이동 시 namespace와 참조 영향을 함께 확인한다.
20. 아키텍처 규칙보다 책임의 명확성과 유지보수성을 최종 기준으로 한다.

---

# 17. 아키텍처 핵심 요약

본 프로젝트의 최상위 구조는 다음 영역을 중심으로 구성한다.

```text
Entity
= 게임의 주체

System
= 특정 Entity에 귀속되지 않는 게임 공용 규칙

LDA
= 외부 데이터 로드 및 저장·조회

Common
= 게임 규칙과 독립적인 범용 기반

Installer
= 상위 범위 객체와 의존성 조립
```

Entity 내부는 필요에 따라 LEA를 적용한다.

```text
CompositionRoot
= Entity 내부 전체 조립 / 외부 연결 / Core

Domain
= 상태와 개념

Flow
= 판단과 행동 흐름

Module
= Entity 내부 기능 실행

View
= 외부 표현
```

Manager는 별도의 아키텍처 영역이 아니다.

```text
Manager
= 특정 소유 영역 내부에서 대상을 관리하는 객체 역할
```

본 프로젝트의 가장 중요한 분류 원칙:

> 먼저 코드의 소유 주체를 판단하고, 이후 책임을 분류한다.

아키텍처의 최종 목적:

> 새로운 코드를 어디에 작성해야 하는지 예측할 수 있고, 기존 코드를 수정할 때 영향 범위를 쉽게 파악할 수 있는 구조를 만든다.

구조 자체를 유지하는 것이 목적이 아니다.

책임이 명확하고 유지보수하기 쉬운 코드를 만드는 것이 목적이다.
