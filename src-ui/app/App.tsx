import { useEffect } from "react";
import type { QueryClient } from "@tanstack/react-query";
import { ReminderWindow } from "../features/settings/ReminderWindow";
import { SettingsWindow } from "../features/settings/SettingsWindow";

type AppProps = {
  queryClient: QueryClient;
};

export function App({ queryClient }: AppProps) {
  const searchParams = new URLSearchParams(window.location.search);
  const windowType = searchParams.get("window");

  useEffect(() => {
    const nextWindowType = windowType === "reminder" ? "reminder" : "main";
    document.documentElement.dataset.window = nextWindowType;
    document.body.dataset.window = nextWindowType;

    return () => {
      delete document.documentElement.dataset.window;
      delete document.body.dataset.window;
    };
  }, [windowType]);

  if (windowType === "reminder") {
    return <ReminderWindow />;
  }

  return <SettingsWindow queryClient={queryClient} />;
}
