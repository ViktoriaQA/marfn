import { DeleteOutlined } from "@ant-design/icons";
import type { DeleteButtonProps } from "./types";
import "./DeleteButton.scss";

const DeleteButton = ({ onClick, isLoading = false }: DeleteButtonProps) => {
  return (
    <button
      className="delete-button"
      onClick={onClick}
      disabled={isLoading}
      title="Remove participant"
      type="button"
    >
      <DeleteOutlined />
    </button>
  );
};

export default DeleteButton;
