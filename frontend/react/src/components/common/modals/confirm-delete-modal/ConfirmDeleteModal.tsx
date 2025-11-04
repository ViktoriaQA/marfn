import { useEffect } from "react";
import { createPortal } from "react-dom";
import Button from "@components/common/button/Button";
import type { ConfirmDeleteModalProps } from "./types";
import "@assets/styles/common/modal-container.scss";
import "./ConfirmDeleteModal.scss";

const ConfirmDeleteModal = ({
  isOpen,
  onClose,
  onConfirm,
  participantName,
}: ConfirmDeleteModalProps) => {
  useEffect(() => {
    document.body.style.overflow = isOpen ? "hidden" : "";

    return () => {
      document.body.style.overflow = "";
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const modalElement = (
    <div className="modal-container">
      <div className="confirm-delete-modal">
        <div className="confirm-delete-modal__top">
          <h3 className="confirm-delete-modal__title">Remove Participant?</h3>
        </div>

        <div className="confirm-delete-modal__bottom">
          <p className="confirm-delete-modal__description">
            Are you sure you want to remove {participantName} from the room?
          </p>
          <p className="confirm-delete-modal__warning">
            This action cannot be undone.
          </p>

          <div className="confirm-delete-modal__buttons">
            <Button
              size="medium"
              variant="secondary"
              width={120}
              onClick={onClose}
            >
              Cancel
            </Button>
            <Button
              size="medium"
              variant="primary"
              width={120}
              onClick={onConfirm}
              className="confirm-delete-modal__remove-button"
            >
              Remove
            </Button>
          </div>
        </div>
      </div>
    </div>
  );

  return createPortal(modalElement, document.body);
};

export default ConfirmDeleteModal;
