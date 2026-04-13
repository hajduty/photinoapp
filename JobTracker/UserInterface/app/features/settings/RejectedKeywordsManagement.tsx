import React from "react";
import { Box, Title, Text, Badge, Group, ActionIcon, Stack } from "@mantine/core";
import { IconX } from "@tabler/icons-react";
import { useRejectedTechKeywords } from "../../hooks/useRejectedTechKeywords";

export function RejectedKeywordsManagement() {
  const { rejectedKeywords, isLoadingRejectedKeywords, rejectedKeywordsError, removeRejectedKeyword } = useRejectedTechKeywords();

  if (isLoadingRejectedKeywords) {
    return <Text>Loading rejected keywords...</Text>;
  }

  if (rejectedKeywordsError) {
    return <Text>Error loading rejected keywords: {rejectedKeywordsError.message}</Text>;
  }

  return (
    <Box>
      <Stack spacing="xs">
        {rejectedKeywords.length === 0 ? (
          <Text size="sm" color="dimmed">No rejected tech keywords.</Text>
        ) : (
          rejectedKeywords.map((tag) => (
            <Badge
              key={tag.Id}
              size="lg"
              variant="light"
              color="red"
              rightSection={
                <ActionIcon
                  size="xs"
                  color="red"
                  radius="xl"
                  variant="transparent"
                  onClick={() => removeRejectedKeyword(tag.Id)}
                >
                  <IconX size={14} />
                </ActionIcon>
              }
            >
              {tag.Name}
            </Badge>
          ))
        )}
      </Stack>
    </Box>
  );
}
