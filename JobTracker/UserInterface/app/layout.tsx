'use client'

import { Inter } from "next/font/google";
import "./globals.css";
import { ColorSchemeScript, MantineProvider, Drawer, Burger } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import { Notifications } from '@mantine/notifications';
import Sidebar from "./components/Sidebar";

const inter = Inter({ subsets: ["latin"] });

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const [opened, { open, close }] = useDisclosure(false);

  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <ColorSchemeScript defaultColorScheme="dark"/>
        <title>JobTracker</title>
        <meta name="description" content="Job tracking application" />
      </head>
      <body className={`${inter.className} min-h-screen bg-neutral-950`}>
        <MantineProvider defaultColorScheme="dark">
          <div className="flex min-h-screen relative">
            {/* Desktop Sidebar - hidden on small screens */}
            <div className="hidden md:block fixed left-0 top-0 bottom-0 z-50">
              <Sidebar />
            </div>

            {/* Mobile Header with Hamburger - visible only on small screens */}
            <div className="md:hidden fixed top-0 left-0 right-0 h-16 bg-neutral-950 border-b border-neutral-700 z-40 flex items-center px-4">
              <Burger opened={opened} onClick={open} color="white" size="sm" />
              <span className="ml-4 text-lg font-semibold text-white">JOBTRACKER</span>
            </div>

            {/* Mobile Drawer */}
            <Drawer
              opened={opened}
              onClose={close}
              size="xs"
              padding={0}
              withCloseButton={false}
              classNames={{
                body: "p-0 h-full",
                content: "bg-neutral-950"
              }}
            >
              <Sidebar onNavigate={close} />
            </Drawer>

            {/* Main Content - no left padding on mobile, padding on desktop */}
            <main className="flex-1 overflow-y-auto pt-16 md:pt-0 md:pl-48 min-h-screen">
              <Notifications />
              {children}
            </main>
          </div>
        </MantineProvider>
      </body>
    </html>
  );
}
