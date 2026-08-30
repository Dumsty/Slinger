using UnityEngine;

public class SelectCard : MonoBehaviour
{
    public PlayerHand playerHand;
    public int activeSlot = -1;

    void Update()
    {
        if (activeSlot == -1)
        {
            int first = 0;
            while (first < 7 && playerHand.hand[first] == null) first++;
            if (first < 7) activeSlot = first;
        }

        for (int i = 0; i < 7; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                int count = -1, target = -1;
                for (int s = 0; s < 7; s++)
                    if (playerHand.hand[s] != null && ++count == i) { target = s; break; }
                if (target != -1) activeSlot = target;
            }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int dir = scroll > 0 ? 1 : -1;
            int next = activeSlot;
            do { next = (next + dir + 7) % 7; }
            while (playerHand.hand[next] == null && next != activeSlot);
            activeSlot = next;
        }

        Card[] active = System.Array.FindAll(playerHand.hand, c => c != null);
        float startX = -(active.Length - 1) * playerHand.cardSpacing / 2f;
        for (int i = 0; i < active.Length; i++)
        {
            float offsetFromCenter = startX + i * playerHand.cardSpacing;
            float t = Mathf.Clamp01(Mathf.Abs(offsetFromCenter) / (playerHand.cardSpacing * (active.Length / 2f) + 0.001f));
            float archY = playerHand.archHeight * playerHand.archCurve.Evaluate(t);
            float z = (playerHand.reverseFan ? (active.Length - 1 - i) : i) * playerHand.depthOffset;
            bool isActive = playerHand.hand[activeSlot] == active[i];

            active[i].transform.localPosition = new Vector3(offsetFromCenter, archY + (isActive ? 0.2f : 0f), isActive ? -0.1f : z);
        }

        if (Input.GetMouseButtonDown(0) && activeSlot != -1 && playerHand.hand[activeSlot] != null)
        {
            int played = activeSlot;
            playerHand.hand[activeSlot].Play();
            playerHand.RemoveCard(activeSlot);

            int left = played - 1;
            while (left >= 0 && playerHand.hand[left] == null) left--;

            int right = played + 1;
            while (right < 7 && playerHand.hand[right] == null) right++;

            activeSlot = left >= 0 ? left : (right < 7 ? right : -1);
        }
    }
}