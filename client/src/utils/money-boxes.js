import axiosInstance from "@/utils/axiosConfig"

export const MONEY_BOX_NONE = "__money_box_none__"
export const MONEY_BOX_UNCATEGORIZED = "__money_box_uncategorized__"
export const ALL_MONEY_BOXES = "__all_money_boxes__"

export async function fetchMoneyBoxesOverview(walletId) {
    const response = await axiosInstance.get("/MoneyBoxes", {
        params: walletId ? { wallet_id: walletId } : {},
    })
    return response?.data ?? {
        items: [],
        count: 0,
        limit: 3,
        can_create: true,
        selection_enabled: true,
        active_plan_id: "free",
        restriction_message: null,
    }
}
