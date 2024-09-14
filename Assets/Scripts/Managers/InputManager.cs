using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    public Vector3Int latestMovement = Vector3Int.back;

    private bool isHolding = false;
    private Coroutine movementCoro = null;
    private readonly float repeatMovementCD = 0.12f;
    private readonly float manualMovementCD = 0.12f;
    private float currentMovementCD = 0f;

    // Hub //
    private Hub hubMovement = null;

    // Debug //
    private bool canInputCommands = false;
    private string confirmCommand = null;
    private string debugCommand = null;

    void Awake()
    {       
        // Singleton without persistence (GameManager already declares it)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Defaults
        latestMovement = Vector3Int.back;
    }

    // Debug input
    void Update()
    {
        if (!canInputCommands) return;

        // Write command
        if (Input.anyKeyDown)
        {
            debugCommand += Input.inputString.Trim().ToLower();
        }

        // Enable debug command
        if (debugCommand == "debug")
        {
            if (!GameManager.Instance.buildDebugMode)
            {
                GameManager.Instance.buildDebugMode = true;
                UI.Instance.global.SendMessage("I hope you know what you're doing.", 3);
                AudioManager.Instance.PlaySFX(AudioManager.areaOverlap, 0.30f);
            } else {
                GameManager.Instance.buildDebugMode = false;
                UI.Instance.global.SendMessage("Good call.", 3);
                AudioManager.Instance.PlaySFX(AudioManager.areaOverlap, 0.30f);
            }
            debugCommand = null;
        }

        // Delete savedata and generate a new one
        if (debugCommand == "begone" && GameManager.Instance.buildDebugMode)
        {
            if (DebugConfirm()) return;
            GameManager.Instance.DeleteSave();
            GameManager.Instance.CreateSave(true);
            AudioManager.Instance.PlaySFX(AudioManager.tilePush, 0.30f);
            UI.Instance.global.SendMessage("Been taken care of.", 4);
            debugCommand = null;
        }
    }

    // Returns if you are past the move cooldown timer
    private bool MoveCDCheck(float cooldown)
    {
        return Time.time < currentMovementCD + cooldown;
    }

    // INGAME (LevelManager) //

    // Player movement
    private void OnMove(InputValue ctx)
    {
        if (!LevelManager.Instance.IsAllowedToPlay()) return;

        // Input prevention logic
        Vector3Int movement = Vector3Int.RoundToInt(ctx.Get<Vector2>());
        if (movement == Vector3Int.zero) { isHolding = false; return; }
        if (movement.x != 0 && movement.y != 0) return;

        // Tile vars
        isHolding = true;
        latestMovement = movement;

        // Move a single time
        if (MoveCDCheck(manualMovementCD) && !GameManager.save.preferences.repeatInput) return;
        if (!GameManager.save.preferences.repeatInput)
        {
            LevelManager.Instance.AddUndoFrame();
            LevelManager.Instance.ApplyGravity(movement);
            
            // New move CD
            currentMovementCD = Time.time;
            return;
        }

        // Repeat your movement
        if (movementCoro != null) StopCoroutine(movementCoro); 
        movementCoro = StartCoroutine(RepeatMovement(movement));
    }

    // Undo move
    private void OnUndo()
    {
        if (!LevelManager.Instance.IsAllowedToPlay() || !LevelManager.Instance.IsUndoQueueValid() || LevelManager.Instance.currentLevel.freeroam) return;
        LevelManager.Instance.Undo();
        LevelManager.Instance.RemoveUndoFrame();
    }

    // Ping all areas
    private void OnPingAreas(InputValue ctx)
    {
        LevelManager.Instance.PingAllAreas(ctx.Get<float>() == 1f);
    }

    // Restart the level
    private void OnRestart()
    {
        if (!LevelManager.Instance.IsAllowedToPlay() || LevelManager.Instance.currentLevel.freeroam) return;
        LevelManager.Instance.ReloadLevel();
    }

    // Repeats a movement
    private IEnumerator RepeatMovement(Vector3Int direction, float speed = 0.005f)
    {
        while (direction == latestMovement && isHolding && LevelManager.Instance.IsAllowedToPlay())
        {
            if (!MoveCDCheck(repeatMovementCD))
            {
                // Move
                LevelManager.Instance.AddUndoFrame();
                LevelManager.Instance.ApplyGravity(direction);
                currentMovementCD = Time.time;
            }
            yield return new WaitForSeconds(speed);
        }
    }

    // External (GameManager) //

    // Pause event
    private void OnPause()
    {
        if (GameManager.Instance.IsBadScene() || LevelManager.Instance.hasWon || DialogManager.Instance.inDialog) return;
        if (!UI.Instance.pause.self.activeSelf) LevelManager.Instance.PauseResumeGame(true);
        else LevelManager.Instance.PauseResumeGame(false);
    }

    // Level Editor (Editor) //

    // Changing tiles
    private void OnEditorSelectWall()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;

        Editor.I.tileToPlace = LevelManager.Instance.wallTile;
    }
    private void OnEditorSelectAntiWall()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;

        Editor.I.tileToPlace = LevelManager.Instance.antiwallTile;
    }
    private void OnEditorSelectBox()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        
        Editor.I.tileToPlace = LevelManager.Instance.boxTile;
    }
    private void OnEditorSelectCircle()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        
        Editor.I.tileToPlace = LevelManager.Instance.circleTile; 
    }  
    private void OnEditorSelectHex() 
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        
        Editor.I.tileToPlace = LevelManager.Instance.hexagonTile;
    }
    private void OnEditorSelectArea()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.areaTile;
    }
    private void OnEditorSelectInverseArea() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.inverseAreaTile; 
    }
    private void OnEditorSelectHazard() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.hazardTile; 
    }
    private void OnEditorSelectInvert() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.invertTile; 
    }
    private void OnEditorSelectArrow() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.arrowTile; 
    }
    private void OnEditorSelectNegativeArrow() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.negativeArrowTile; 
    }
    private void OnEditorSelectOrb() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.orbTile;
    }
    private void OnEditorSelectLevel() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.levelTile; 
    }
    private void OnEditorSelectFake() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.fakeTile; 
    }
    private void OnEditorSelectNPC() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.npcTile;
    }
    private void OnEditorSelectMimic() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.mimicTile; 
    }
    private void OnEditorSelectVoid() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.tileToPlace = LevelManager.Instance.voidTile; 
    }

    // Places a tile
    private void OnEditorClickGrid()
    {
        if (!GameManager.Instance.IsEditor()) return;
        
        // Checks if you are already multi-placing
        if (Editor.I.multiClick != null)
        {
            StopCoroutine(Editor.I.multiClick);
            Editor.I.multiClick = null;
            return;
        }

        // Multi placing tiles !!
        Editor.I.multiClick = StartCoroutine(Editor.I.MultiPlace());
    }

    // Changes a tile's properties (objects only)
    private void OnEditorRightClickGrid()
    {
        if (!GameManager.Instance.IsEditor()) return;
        
        // Checks mouse position
        Vector3Int gridPos = Editor.I.GetMousePositionOnGrid();
        if (gridPos == Vector3.back || UI.Instance.editor.self.activeSelf) return;

        // Tile validation and selection
        Editor.I.editingTile = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(gridPos);
        if (!Editor.I.editingTile) Editor.I.editingTile = LevelManager.Instance.tilemapCustoms.GetTile<CustomTile>(gridPos);
        if (!Editor.I.editingTile) Editor.I.editingTile = LevelManager.Instance.tilemapEffects.GetTile<EffectTile>(gridPos); // Only arrow tiles
        if (!Editor.I.editingTile) { UI.Instance.global.SendMessage($"Invalid tile at position \"{gridPos}\""); return; }

        // Update UI (dear god)
        Editor.I.ignoreUpdateEvent = true;

        // Custom tiles field (missing loading custom text automatically)
        if (LevelManager.Instance.typesCustomsList.Contains(Editor.I.editingTile.GetTileType())) {
            Editor.I.customInputField.interactable = true;
        } else {
            Editor.I.customInputField.interactable = false;
            Editor.I.customInputField.text = string.Empty;
        }

        // Basic
        Editor.I.pushableToggle.interactable = Editor.I.editingTile.directions.editorPushable;
        Editor.I.pushableToggle.isOn = Editor.I.editingTile.directions.pushable;
        Editor.I.upToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.downToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.leftToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.rightToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.upToggle.isOn = Editor.I.editingTile.directions.up;
        Editor.I.downToggle.isOn = Editor.I.editingTile.directions.down;
        Editor.I.leftToggle.isOn = Editor.I.editingTile.directions.left;
        Editor.I.rightToggle.isOn = Editor.I.editingTile.directions.right;
        Editor.I.ignoreUpdateEvent = false;
    }

    // Toggles menu
    private void OnEditorEscape()
    {
        if (!GameManager.Instance.IsEditor()) return;
        if (!UI.Instance || !EventSystem.current) return;

        if (!UI.Instance.editor.self.activeSelf) EventSystem.current.SetSelectedGameObject(UI.Instance.editor.playtest);
        UI.Instance.editor.Toggle(!UI.Instance.editor.self.activeSelf);
    }

    // Select deleting/placing tiles
    private void OnEditorDelete(InputValue ctx)
    {
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.isPlacing = ctx.Get<float>() != 1f;
    }

    // Moving the level screen up/down/left/right
    private void OnEditorUp() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(0, -8));
        LevelManager.Instance.worldOffsetY += 8;
    }

    private void OnEditorDown() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(0, 8));
        LevelManager.Instance.worldOffsetY -= 8;
    }

    private void OnEditorLeft() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(14, 0));
        LevelManager.Instance.worldOffsetX -= 14;
    }

    private void OnEditorRight() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(-14, 0));
        LevelManager.Instance.worldOffsetX += 14;
    }

    // Hub //
    private void OnHubSwipeLeft()
    {
        if (SceneManager.GetActiveScene().name != "Hub") return;
        if (!hubMovement) hubMovement = GameObject.Find("Hub UI").GetComponent<Hub>();
        hubMovement.ChangeWorld(-1);
    }

    private void OnHubSwipeRight()
    {
        if (SceneManager.GetActiveScene().name != "Hub") return;
        if (!hubMovement) hubMovement = GameObject.Find("Hub UI").GetComponent<Hub>();
        hubMovement.ChangeWorld(1);
    }

    // Etc //

    private void OnInteract()
    {
        if (GameManager.Instance.IsBadScene() || LevelManager.Instance.isPaused) return;

        // Searches for a first valid NPC
        GameTile npc = LevelManager.Instance.GetCustomTiles().Find(
            npc => {
                GameTile test = null;
                test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(1, 0, 0));
                if (!test) test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(-1, 0, 0));
                if (!test) test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(0, 1, 0));
                if (!test) test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(0, -1, 0));
                if (test) return npc;
                else return false;
            }
        );

        // Gets the NPC and triggers it
        if (npc) { LevelManager.Instance.tilemapCustoms.GetTile<NPCTile>(npc.position).Effect(null); }
    }

    // Debug commands //
    
    // Enable debug commands
    private void OnDebugEnable(InputValue ctx)
    {
        if (SceneManager.GetActiveScene().name != "Main Menu") return;
        canInputCommands = ctx.Get<float>() == 1f;

        if (!canInputCommands) debugCommand = null;
    }

    // Confirm a command
    private bool DebugConfirm()
    {
        if (confirmCommand != debugCommand)
        {
            UI.Instance.global.SendMessage("Are you sure about that?", 2);
            AudioManager.Instance.PlaySFX(AudioManager.tileDeath, 0.30f);
            confirmCommand = $"{debugCommand}";
            debugCommand = null;
            return true;
        }
        
        confirmCommand = null;
        return false;
    }
}
