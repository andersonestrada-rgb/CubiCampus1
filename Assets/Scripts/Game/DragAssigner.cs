using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Interaccion principal: arrastrar estudiantes a cualquier mesa (ideal o no),
/// liberar mesas con clics repetidos, limpiar apartados y raspar manchas.
/// Usa el nuevo Input System y colliders 2D.
/// </summary>
public class DragAssigner : MonoBehaviour
{
    public GameManager game;
    public Camera cam;

    Student dragging;
    Seat hoverSeat;
    Seat scrubbingSeat;
    Vector3 dragOffset;
    Vector3 lastScrubWorld;
    bool relocateMode;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (game == null) game = GameManager.Instance;
        if (game == null) return;

        if (!game.IsPlaying || game.IsPaused)
        {
            if (dragging != null) AbortDrag();
            StopScrub();
            return;
        }

        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        Vector3 world = ScreenToWorld(pointer.position.ReadValue());

        if (dragging != null && (dragging.state == StudentState.Gone || dragging.state == StudentState.Leaving))
        {
            dragging = null;
            ClearHover();
            return;
        }

        // --- raspado de manchas: mantener clic y mover el cursor ---
        if (scrubbingSeat != null)
        {
            if (pointer.press.isPressed) ContinueScrub(world);
            else StopScrub();
        }

        if (pointer.press.wasPressedThisFrame)
        {
            OnPress(world);
        }
        else if (pointer.press.isPressed)
        {
            if (dragging != null && dragging.state == StudentState.Dragging)
            {
                dragging.transform.position = world + dragOffset;
                UpdateHover(world);
            }
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            if (dragging != null) OnRelease(world);
        }
    }

    Vector3 ScreenToWorld(Vector2 screen)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
        p.z = 0f;
        return p;
    }

    void OnPress(Vector3 world)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Student st = FindStudent(world);
        if (st != null)
        {
            if (st.state == StudentState.InQueue)
            {
                StartDrag(st, world, false);
            }
            else if (st.state == StudentState.Ready)
            {
                // Primer clic: empieza a recoger. Los siguientes aceleran la salida.
                st.BeginPacking();
                st.PackClick();
            }
            else if (st.state == StudentState.Packing)
            {
                st.PackClick();
            }
            else if (st.needsAttention)
            {
                st.ResolveDistraction();
            }
            else if (st.state == StudentState.Seated && game.RelocationAvailable)
            {
                StartDrag(st, world, true);
            }
            return;
        }

        Seat seat = FindSeat(world);
        if (seat == null) return;

        if (seat.blocked)
        {
            seat.StartClean();
            game.SetMessage("Limpiando apartado (2s)...", 1.4f);
        }
        else if (seat.dirty)
        {
            // Las manchas se quitan raspando: mantener clic y mover el cursor.
            StartScrub(seat, world);
        }
        else if (seat.HasReadyOccupant)
        {
            ClickReadyOccupants(seat);
        }
        else if (seat.occupants.Count > 0)
        {
            game.SetMessage("Ese estudiante sigue estudiando", 1f);
        }
    }

    void ClickReadyOccupants(Seat seat)
    {
        bool clicked = false;

        for (int i = 0; i < seat.occupants.Count; i++)
        {
            Student o = seat.occupants[i];
            if (o == null) continue;

            if (o.state == StudentState.Ready)
            {
                o.BeginPacking();
                o.PackClick();
                clicked = true;
            }
            else if (o.state == StudentState.Packing)
            {
                o.PackClick();
                clicked = true;
            }
        }

        if (!clicked) game.SetMessage("Ese estudiante sigue estudiando", 1f);
    }

    // ------------------------------------------------------------------ raspado

    void StartScrub(Seat seat, Vector3 world)
    {
        scrubbingSeat = seat;
        lastScrubWorld = world;
        seat.SetHighlight(4);
        game.SetMessage("Manten el clic y mueve el cursor para raspar la mancha", 1.8f);
    }

    void ContinueScrub(Vector3 world)
    {
        if (scrubbingSeat == null) return;

        // Si el cursor sale de la mesa dejamos de raspar.
        Seat under = FindSeat(world);
        if (under != scrubbingSeat)
        {
            StopScrub();
            return;
        }

        float moved = Vector2.Distance(lastScrubWorld, world);
        lastScrubWorld = world;
        if (moved < 0.0005f) return;

        bool clean = scrubbingSeat.Scrub(moved);

        if (clean)
        {
            scrubbingSeat.SetHighlight(0);
            scrubbingSeat = null;
            game.SetMessage("Mesa limpia", 1.2f);
        }
    }

    void StopScrub()
    {
        if (scrubbingSeat != null) scrubbingSeat.SetHighlight(0);
        scrubbingSeat = null;
    }

    // ------------------------------------------------------------------ arrastre

    void StartDrag(Student st, Vector3 world, bool relocate)
    {
        dragging = st;
        relocateMode = relocate;
        dragOffset = st.transform.position - world;
        st.BeginDrag();
    }

    void UpdateHover(Vector3 world)
    {
        Seat seat = FindSeat(world);

        if (hoverSeat != null && hoverSeat != seat) 
        {
            hoverSeat.SetHighlight(0);
            hoverSeat.ClearDragHighlight();
        }

        hoverSeat = seat;

        if (seat == null || dragging == null) return;

        if (relocateMode && dragging.seat == seat)
        {
            seat.ClearDragHighlight();
            seat.SetHighlight(0);
            return;
        }

        if (!seat.CanAccept(dragging.profile))
        {
            seat.ClearDragHighlight();
            seat.SetHighlight(2);
        }
        else
        {
            bool ideal = seat.MatchesProfile(dragging.profile);
            seat.SetDragHighlight(true, ideal);
            seat.SetHighlight(ideal ? 1 : 3);
        }
    }

    void OnRelease(Vector3 world)
    {
        Seat seat = FindSeat(world);
        Student st = dragging;
        dragging = null;
        ClearHover();

        if (st == null) return;
        if (st.state == StudentState.Leaving || st.state == StudentState.Gone) return;

        // Soltar sobre la misma mesa no cuesta reubicacion ni penalizacion.
        if (relocateMode && st.seat == seat)
        {
            st.ReturnToSeat();
            return;
        }

        if (CanDropOn(st, seat))
        {
            bool ideal = seat.MatchesProfile(st.profile);

            if (relocateMode) game.RegisterRelocate();
            st.AssignTo(seat);

            if (ideal) game.SetMessage("Asignado a " + seatName(seat), 1.2f);
            else game.SetMessage("Mesa inadecuada: menos puntos (" + seat.MismatchReason(st.profile) + ")", 2f);
            return;
        }

        if (seat != null)
        {
            game.RegisterBadAssignment();
            game.SetMessage(seat.RejectReason(st.profile), 1.6f);
        }

        if (relocateMode)
        {
            st.ReturnToSeat();
        }
        else
        {
            int index = game.queue != null ? game.queue.Students.IndexOf(st) : 0;
            st.CancelDrag(game.queue != null ? game.queue.SlotPosition(Mathf.Max(0, index)) : st.transform.position);
        }
    }

    bool CanDropOn(Student st, Seat seat)
    {
        if (seat == null || st == null) return false;
        if (relocateMode && st.seat == seat) return false;
        return seat.CanAccept(st.profile);
    }

    string seatName(Seat s)
    {
        switch (s.kind)
        {
            case SeatKind.Doble: return "mesa doble";
            case SeatKind.Ruido: return "zona de ruido";
            default: return "cubico " + s.seatId;
        }
    }

    void ClearHover()
    {
        if (hoverSeat != null)
        {
            hoverSeat.SetHighlight(0);
            hoverSeat.ClearDragHighlight();
            hoverSeat = null;
        }
    }

    // Cancela un arrastre en curso sin penalizar (pausa, fin de turno o abandono).
    void AbortDrag()
    {
        Student st = dragging;
        dragging = null;
        ClearHover();

        if (st == null) return;
        if (st.state == StudentState.Leaving || st.state == StudentState.Gone) return;

        if (relocateMode)
        {
            st.ReturnToSeat();
        }
        else
        {
            int index = game.queue != null ? game.queue.Students.IndexOf(st) : 0;
            st.CancelDrag(game.queue != null ? game.queue.SlotPosition(Mathf.Max(0, index)) : st.transform.position);
        }
    }

    Student FindStudent(Vector3 world)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        Student best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Student s = hits[i].GetComponent<Student>();
            if (s == null || s.state == StudentState.Leaving || s.state == StudentState.Gone) continue;

            if (s.state == StudentState.Walking && s.seat == null) continue;

            if (best == null || ((Vector2)(s.transform.position - world)).sqrMagnitude <
                                 ((Vector2)(best.transform.position - world)).sqrMagnitude)
            {
                best = s;
            }
        }

        return best;
    }

    Seat FindSeat(Vector3 world)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        Seat best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Seat s = hits[i].GetComponent<Seat>();
            if (s == null) continue;
            best = s;
            break;
        }

        return best;
    }
}
