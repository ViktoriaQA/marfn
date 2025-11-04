import type { ButtonHTMLAttributes } from "react";

export type IconName =
  | "copy"
  | "edit"
  | "save"
  | "info"
  | "link"
  | "cross"
  | "delete";
export type IconColor = "green" | "white" | "red";

export type IconButtonProps = {
  iconName: IconName;
  color?: IconColor;
  onClick: () => void;
} & ButtonHTMLAttributes<HTMLButtonElement>;
