using ClickableTransparentOverlay;
using ESPUnturned;
using ImGuiNET;
using Swed64;
using System.Numerics;
using System.Runtime.InteropServices;

namespace UnturnedESP
{
    class Program : Overlay
    {
        // External native function to get window position
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int left, top, right, bottom; }

        // Game memory & entity state
        private readonly Swed swed = new Swed("Unturned");
        private readonly Offsets offsets = new Offsets();

        private readonly List<Entity> entities = new List<Entity>();
        private Entity localPlayer = new Entity();

        private IntPtr viewMatrixAddress = (IntPtr)0x21C2BEBFC1C;
        private float[] viewMatrix = new float[16];
        private IntPtr viewMatrixPtr = IntPtr.Zero;

        private IntPtr client;
        private IntPtr entityListPtr;
        private IntPtr entityItemsPtr;
        private int entityCount;

        // UI rendering
        private ImDrawListPtr drawList;
        private Vector2 windowLocation = new Vector2(0, 0);
        private Vector2 windowSize = new Vector2(1920, 1080);
        private Vector2 lineOrigin;
        private Vector2 windowCenter;

        private Vector4 enemyColor = new Vector4(1, 0, 0, 1);

        private bool enableEsp = true;
        private bool showBox = true, showLine = true, showDot = true, showDistance = true;

        protected override void Render()
        {
            
            viewMatrixPtr = swed.ReadPointer(client, 0x01A7C580, 0x40, 0x90, 0x548, 0xE40, 0x78, 0x40);
            viewMatrixPtr += 0xdc;
            viewMatrix = swed.ReadMatrix(viewMatrixPtr);
            DrawMenu();
            DrawOverlay();
            DrawEsp();
            ImGui.End();
        }

        private void DrawEsp()
        {
            drawList = ImGui.GetBackgroundDrawList();

            if (!enableEsp) return;

            lock (entities)
            {
                foreach (var entity in entities)
                {
                    if (!IsPixelInsideScreen(entity.originScreenPosition)) continue;
                    DrawEntityVisuals(entity);
                }
            }
        }

        private void DrawEntityVisuals(Entity entity)
        {
            Vector2 boxSize = new Vector2((entity.originScreenPosition.Y - entity.absScreenPosition.Y) / 2f, 0f);
            Vector2 boxStart = entity.absScreenPosition - boxSize;
            Vector2 boxEnd = entity.originScreenPosition + boxSize;
            uint color = ImGui.ColorConvertFloat4ToU32(enemyColor);

            if (showLine) drawList.AddLine(lineOrigin, entity.originScreenPosition, color, 2);
            if (showBox) drawList.AddRect(boxStart, boxEnd, color, 2);
            if (showDot) drawList.AddCircleFilled(entity.originScreenPosition, 5f, color);
            if (showDistance)
                drawList.AddText(entity.originScreenPosition, color, $"{entity.magnitude:F1}m");
        }

        private bool IsPixelInsideScreen(Vector2 pixel)
        {
            return pixel.X >= windowLocation.X && pixel.X <= windowLocation.X + windowSize.X &&
                   pixel.Y >= windowLocation.Y && pixel.Y <= windowLocation.Y + windowSize.Y;
        }

        private Vector2 WorldToScreen(Vector3 pos)
        {
            float clipX = viewMatrix[0] * pos.X + viewMatrix[4] * pos.Y + viewMatrix[8] * pos.Z + viewMatrix[12];
            float clipY = viewMatrix[1] * pos.X + viewMatrix[5] * pos.Y + viewMatrix[9] * pos.Z + viewMatrix[13];
            float clipW = viewMatrix[3] * pos.X + viewMatrix[7] * pos.Y + viewMatrix[11] * pos.Z + viewMatrix[15];

            if (clipW < 0.1f) return new Vector2(-99, -99);

            float ndcX = clipX / clipW;
            float ndcY = clipY / clipW;

            float screenX = (ndcX + 1f) * windowSize.X / 2f + windowLocation.X;
            float screenY = (1f - ndcY) * windowSize.Y / 2f + windowLocation.Y;

            return new Vector2(screenX, screenY);
        }

        private void DrawMenu()
        {
            ImGui.Begin("Unturned ESP by attackN SOLUTIONS");
            if (ImGui.BeginTabBar("MainTabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Checkbox("Enable ESP", ref enableEsp);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Visuals"))
                {
                    ImGui.ColorPicker4("Enemy Color", ref enemyColor);
                    ImGui.Checkbox("Show Boxes", ref showBox);
                    ImGui.Checkbox("Show Lines", ref showLine);
                    ImGui.Checkbox("Show Dots", ref showDot);
                    ImGui.Checkbox("Show Distance", ref showDistance);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
            ImGui.End();
        }

        private void DrawOverlay()
        {
            ImGui.SetNextWindowSize(windowSize);
            ImGui.SetNextWindowPos(windowLocation);
            ImGui.Begin("##overlay",
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse);
        }

        private void MainLogic()
        {
            RECT rect;
            if (!GetWindowRect(swed.GetProcess().MainWindowHandle, out rect)) return;
            windowLocation = new Vector2(rect.left, rect.top);
            windowSize = new Vector2(rect.right - rect.left, rect.bottom - rect.top);
            lineOrigin = new Vector2(windowLocation.X + windowSize.X / 2f, windowLocation.Y + windowSize.Y);
            windowCenter = new Vector2(lineOrigin.X, windowLocation.Y + windowSize.Y / 2f);

            client = swed.GetModuleBase("UnityPlayer.dll");
            entityListPtr = swed.ReadPointer(client, 0x01B39E08, 0xD0, 0x8, 0xC0, 0x178, 0x2E4, 0x8);
            entityItemsPtr = swed.ReadPointer(entityListPtr, offsets._items);
            entityCount = swed.ReadInt(entityListPtr, offsets._size);

            while (true) UpdateEntityList();
        }

        private void UpdateEntityList()
        {
            lock (entities)
            {
                entities.Clear();

                for (int i = 0; i < entityCount; i++)
                {
                    IntPtr ptr = swed.ReadPointer(entityItemsPtr, 0x20 + i * 0x8);
                    if (ptr == IntPtr.Zero) continue;

                    Entity entity = new Entity { address = ptr };
                    UpdateEntity(ref entity);

                    if (IsLocalPlayer(entity)) localPlayer = entity;
                    entities.Add(entity);
                    
                }
            }
        }

        private void UpdateEntity(ref Entity entity)
        {
            entity._player = swed.ReadPointer(entity.address, offsets._player);
            entity._movement = swed.ReadPointer(entity._player, offsets._movement);
            entity._cachedPtr = swed.ReadPointer(entity._movement, offsets._cachedPtr);
            entity._playerMovement = swed.ReadPointer(entity._cachedPtr, offsets._playerMovement);
            entity.origin = swed.ReadVec(entity._playerMovement, offsets.originX);

            entity._life = swed.ReadPointer(entity._player, offsets._life);
            entity.health = swed.ReadBytes(entity._life, offsets.health, 1)[0];
            entity.stamina = swed.ReadBytes(entity._life, offsets.stamina, 1)[0];
            entity.isDead = swed.ReadBytes(entity._life, offsets.isDead, 1)[0];

            entity.viewOffset = new Vector3(0, 1.75f, 0);
            entity.abs = entity.origin + entity.viewOffset;
            entity.originScreenPosition = WorldToScreen(entity.origin);
            entity.absScreenPosition = WorldToScreen(entity.abs);
            entity.magnitude = Vector3.Distance(localPlayer.origin, entity.origin);
        }

        private bool IsLocalPlayer(Entity entity)
        {
            IntPtr channel = swed.ReadPointer(entity._player, offsets._channel);
            return swed.ReadBytes(channel, offsets.isOwner, 1)[0] == 1;
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Start().Wait();
            Thread t = new Thread(program.MainLogic) { IsBackground = true };
            t.Start();
        }
    }
}
