import { useState } from "react";
import { BASE_API_URL } from "@utils/general";

export const useDeleteUser = () => {
  const [isDeleting, setIsDeleting] = useState(false);

  const deleteUser = async (
    userId: number,
    userCode: string,
  ): Promise<boolean> => {
    setIsDeleting(true);

    try {
      const response = await fetch(
        `${BASE_API_URL}/api/users/${userId}?userCode=${userCode}`,
        {
          method: "DELETE",
          headers: {
            "Content-Type": "application/json",
          },
        },
      );

      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }

      return true;
    } catch (error) {
      console.error("Error deleting user:", error);
      return false;
    } finally {
      setIsDeleting(false);
    }
  };

  return { deleteUser, isDeleting };
};
