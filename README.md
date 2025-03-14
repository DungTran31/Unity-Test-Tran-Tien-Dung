## Task 1: Update Prefabs
- Truy cập thư mục: `Assets/Resources/prefabs`
- Chọn từng prefab có tên `itemNormal0_`
- Đổi `Sprite` thành `fish_1`
- Thực hiện tương tự với các prefab `itemNormal` khác

---

## Task 2: Game Mechanics
### Gameplay Rules
1. Di chuyển các vật phẩm từ bảng xuống ô đáy bằng cách nhấn vào chúng.
2. Khi vật phẩm đã di chuyển xuống ô đáy, không thể di chuyển ngược lại lên bảng.
3. Nếu có đúng **ba vật phẩm giống nhau** trong ô đáy, chúng sẽ bị xóa.
4. Dọn sạch bảng để chiến thắng.
5. Người chơi sẽ thua nếu lấp đầy tất cả các ô đáy.

### Requirements
- Số lượng vật phẩm giống nhau trên bảng ban đầu phải chia hết cho **3**.
- Vùng đáy chứa tối đa **5 ô**.
- Hiển thị **màn hình chiến thắng** khi người chơi thắng.
- Hiển thị **màn hình thua** khi người chơi thua.
- Tạo **màn hình chính** với nút **'Autoplay'**: Khi nhấn, trò chơi tự động chơi đến khi thắng (mỗi hành động có độ trễ **0.5s**).
- Thêm nút **'Auto Lose'**: Khi nhấn, trò chơi tự động chơi đến khi thua (mỗi hành động có độ trễ **0.5s**).

---

## Code Implementation

### **Board.cs** 
Tạo bottom cells
```csharp
public List<Cell> m_bottomCells;
private int m_bottomSize = 5;
private Dictionary<Item, Vector2Int> m_originalPositions;

public Board(Transform transform, GameSettings gameSettings)
{
    m_root = transform;
    m_matchMin = gameSettings.MatchesMin;
    this.boardSizeX = gameSettings.BoardSizeX;
    this.boardSizeY = gameSettings.BoardSizeY;
    
    m_cells = new Cell[boardSizeX, boardSizeY];
    m_originalPositions = new Dictionary<Item, Vector2Int>();
    
    CreateBoard();
    CreateBottomCells();
}

private void CreateBottomCells()
{
    m_bottomCells = new List<Cell>();
    Vector3 origin = new Vector3(-2f, -boardSizeY * 0.5f - 1f, 0f);
    GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
    
    for (int i = 0; i < m_bottomSize; i++)
    {
        GameObject go = GameObject.Instantiate(prefabBG);
        go.transform.position = origin + new Vector3(i, 0, 0f);
        go.transform.SetParent(m_root);
        
        Cell cell = go.GetComponent<Cell>();
        cell.Setup(i, -1);
        
        m_bottomCells.Add(cell);
    }
}
```

#### Kiểm tra trạng thái bottom cells
```csharp
// Kiểm tra ô đáy có đầy không
public bool IsBottomAreaFull()
{
    return m_bottomCells.All(cell => !cell.IsEmpty);
}

// Kiểm tra ô đáy có rỗng không
public bool IsBottomAreaEmpty()
{
    return !m_bottomCells.Any(cell => !cell.IsEmpty);
}
```

#### Di chuyển item xuống bottom cells
```csharp
public void AddItemToBottomArea(Item item)
{
    foreach (var cell in m_bottomCells)
    {
        if (cell.IsEmpty)
        {
            m_originalPositions[item] = new Vector2Int(item.Cell.BoardX, item.Cell.BoardY);
            cell.Assign(item);
            item.View.DOMove(cell.transform.position, 0.3f).OnComplete(() => {
                item.SetViewPosition(cell.transform.position);
            });
            break;
        }
    }
}
```

#### Kiểm tra & xóa các item giống nhau
```csharp
public List<Cell> CheckBottomAreaForMatches()
{
    var matches = new List<Cell>();
    var groups = new List<List<Cell>>();
    
    foreach (var cell in m_bottomCells)
    {
        if (cell.IsEmpty) continue;
        
        bool added = false;
        foreach (var group in groups)
        {
            if (group.Count > 0 && cell.IsSameType(group[0]))
            {
                group.Add(cell);
                added = true;
                break;
            }
        }
        
        if (!added)
        {
            groups.Add(new List<Cell> { cell });
        }
    }
    
    foreach (var group in groups)
    {
        if (group.Count >= 3)
        {
            matches.AddRange(group);
        }
    }
    
    return matches;
}
```

```csharp
public void ClearMatchedBottomArea()
{
    var matches = CheckBottomAreaForMatches();
    foreach (var cell in matches)
    {
        cell.Item.View.DOScale(Vector3.zero, 1f).OnComplete(() => {
            cell.Clear();
        });
    }
}
```

---

### **BoardController.cs** - Auto Play Functions
#### Tự động chơi đến khi thắng
```csharp
public void MakeBestMove()
{
    for (int x = 0; x < m_board.boardSizeX; x++)
    {
        for (int y = 0; y < m_board.boardSizeY; y++)
        {
            var cell = m_board.m_cells[x, y];
            if (!cell.IsEmpty)
            {
                var item = cell.Item;
                
                if (m_board.IsBottomAreaEmpty())
                {
                    m_board.AddItemToBottomArea(item);
                    cell.Free();
                    return;
                }
                
                bool isDuplicate = m_board.m_bottomCells.Any(bottomCell => bottomCell.Item != null && bottomCell.Item.IsSameType(item));
                if (isDuplicate)
                {
                    m_board.AddItemToBottomArea(item);
                    cell.Free();
                    
                    var matches = m_board.CheckBottomAreaForMatches();
                    if (matches.Count > 0)
                    {
                        foreach (var match in matches)
                        {
                            match.ExplodeItem();
                            match.Free();
                        }
                    }
                    
                    if (m_board.IsBottomAreaFull())
                    {
                        ShowLoseScreen();
                        m_gameOver = true;
                    }
                    else if (IsBoardCleared())
                    {
                        ShowWinScreen();
                        m_gameOver = true;
                    }
                    return;
                }
            }
        }
    }
}
```

#### Tự động chơi đến khi thua
```csharp
public void MakeLoseMove()
{
    for (int x = 0; x < m_board.boardSizeX; x++)
    {
        for (int y = 0; y < m_board.boardSizeY; y++)
        {
            var cell = m_board.m_cells[x, y];
            if (!cell.IsEmpty)
            {
                var item = cell.Item;
                bool isDuplicate = m_board.m_bottomCells.Any(bottomCell => bottomCell.Item != null && bottomCell.Item.IsSameType(item));
                
                if (!isDuplicate)
                {
                    m_board.AddItemToBottomArea(item);
                    cell.Free();
                    return;
                }
            }
        }
    }
}
```


## Hàm Update - Kiểm Soát Thao Tác & Logic Game
```csharp
public void Update()
{
    if (m_gameOver || IsBusy) return;

    if (Input.GetMouseButtonDown(0))
    {
        var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider != null)
        {
            m_isDragging = true;
            m_hitCollider = hit.collider;
        }
    }

    if (Input.GetMouseButtonUp(0))
    {
        if (m_isDragging && m_hitCollider != null)
        {
            var cell = m_hitCollider.GetComponent<Cell>();
            if (cell != null && !cell.IsEmpty)
            {
                var item = cell.Item;
                
                if (m_board.m_bottomCells.Contains(cell) && m_gameManager.LevelMode == GameManager.eLevelMode.TIMER)
                {
                    m_board.MoveItemFromBottomToOriginal(cell);
                }
                else
                {
                    cell.Free();
                    m_board.AddItemToBottomArea(item);
                    
                    var matches = m_board.CheckBottomAreaForMatches();
                    if (matches.Count > 0)
                    {
                        foreach (var match in matches)
                        {
                            match.ExplodeItem();
                            match.Free();
                        }
                    }

                    if (m_board.IsBottomAreaFull() && m_gameManager.LevelMode != GameManager.eLevelMode.TIMER)
                    {
                        ShowLoseScreen();
                        m_gameOver = true;
                    }
                    else if (IsBoardCleared())
                    {
                        ShowWinScreen();
                        m_gameOver = true;
                    }
                }
            }
        }
        ResetRayCast();
    }
}
```

## Kiểm Tra Board Đã Rỗng
```csharp
public bool IsBoardCleared()
{
    for (int x = 0; x < m_board.boardSizeX; x++)
    {
        for (int y = 0; y < m_board.boardSizeY; y++)
        {
            if (!m_board.m_cells[x, y].IsEmpty)
            {
                return false;
            }
        }
    }
    return true;
}
```

## Hiển Thị Màn Hình Thắng & Thua
```csharp
private void ShowWinScreen()
{
    m_gameManager.ShowWinScreen();
}

private void ShowLoseScreen()
{
    m_gameManager.ShowLoseScreen();
}
```

---

## GameManager.cs
```csharp
[SerializeField] private GameObject winScreen;
[SerializeField] private GameObject loseScreen;
private bool isAutoplaying = false;
private bool isAutoLosing = false;

public void StartAutoplay()
{
    if (State == eStateGame.GAME_STARTED)
    {
        isAutoplaying = true;
        StartCoroutine(AutoplayCoroutine());
    }
}

private IEnumerator AutoplayCoroutine()
{
    while (isAutoplaying && State == eStateGame.GAME_STARTED)
    {
        m_boardController.MakeBestMove();
        yield return new WaitForSeconds(0.5f);
        
        if (m_boardController.IsBoardCleared())
        {
            ShowWinScreen();
            isAutoplaying = false;
        }
    }
}
```

---

## UI Main Manager
```csharp
internal void StartAutoplay()
{
    m_gameManager.StartAutoplay();
}

internal void StartAutoLose()
{
    m_gameManager.StartAutoLose();
}
```

## Task 3 - Time Attack Mode

### Hoàn Thành:
1. **Đảm bảo board chứa tất cả loại cá ngay từ đầu** (*Đã thực hiện ở hàm `Fill` - Task 2*)
2. **Thêm animation khi item di chuyển và bị xóa** (*Đã thực hiện ở `AddItemToBottomArea` & `ClearMatchedBottomArea` - Task 2*)

### Thêm Chế Độ Time Attack
- **Thêm nút 'Time Attack' vào Home Screen** (*Gán `btnTimer` vào `PanelHome`*)
- **Người chơi không thua nếu bottom cells đầy**
  ```csharp
  if (m_board.IsBottomAreaFull())
  {
      if (m_gameManager.LevelMode != GameManager.eLevelMode.TIMER)
      {
          ShowLoseScreen();
          m_gameOver = true;
      }
  }
  ```
- **Thua nếu không hoàn thành trong 1 phút**
  - Cập nhật `LevelTime = 60` trong `GameSettings`
  - Cập nhật `LevelTime.cs`:
    ```csharp
    protected override void OnConditionComplete()
    {
        base.OnConditionComplete();
        m_mngr.ShowLoseScreen();
        m_mngr.HideWinScreen();
    }
    ```

**Lưu ý:** Cần thêm `virtual` cho `OnConditionComplete()` trong `LevelCondition.cs`.

