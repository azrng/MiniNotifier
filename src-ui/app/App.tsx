import type { QueryClient } from "@tanstack/react-query";
import { ReminderWindow } from "../features/settings/ReminderWindow";
import { SettingsWindow } from "../features/settings/SettingsWindow";

type AppProps = {
  queryClient: QueryClient;
};

export function App({ queryClient }: AppProps) {
  const searchParams = new URLSearchParams(window.location.search);
  const windowType = searchParams.get("window");

  if (windowType === "reminder") {
    return <ReminderWindow />;
  }

  return <SettingsWindow queryClient={queryClient} />;
}
