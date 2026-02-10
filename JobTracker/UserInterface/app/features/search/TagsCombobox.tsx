import {
  Combobox,
  PillsInput,
  Pill,
  useCombobox,
  CheckIcon,
} from '@mantine/core';
import { getContrastColor } from '../../utils/getContrastColor';

export interface Tag {
  Id: number;
  Name: string;
  Color: string;
}

interface TagsComboboxProps {
  tags: Tag[];
  value: Tag[];
  onChange: (tags: Tag[]) => void;
  label: string;
  placeholder?: string;
  maxVisible?: number;
}

export function TagsCombobox({
  tags,
  value,
  onChange,
  label,
  placeholder = 'None',
  maxVisible = 2,
}: TagsComboboxProps) {
  const combobox = useCombobox({
    onDropdownClose: () => combobox.resetSelectedOption(),
  });

  const visible = value.slice(0, maxVisible);
  const hiddenCount = value.length - visible.length;

  const toggleTag = (tag: Tag) => {
    const exists = value.some((t) => t.Id === tag.Id);
    onChange(
      exists
        ? value.filter((t) => t.Id !== tag.Id)
        : [...value, tag]
    );
  };

  return (
    <Combobox store={combobox} withinPortal={false}>
      <Combobox.Target>
        <PillsInput
          label={label}
          onClick={() => combobox.toggleDropdown()}
          styles={{
            input: {
              height: 36,
              minHeight: 36,
              maxHeight: 36,
              padding: '0 8px',
              display: 'flex',
              alignItems: 'center',
            }
          }}
        >
          {/* Selected pills */}
          {visible.map((tag) => (
            <Pill
              key={tag.Id}
              withRemoveButton
              onRemove={() => toggleTag(tag)}
              onClick={(e) => {
                e.stopPropagation();
                toggleTag(tag);
              }}
              style={{
                backgroundColor: tag.Color,
                color: getContrastColor(tag.Color),
                cursor: 'pointer',
              }}
            >
              {tag.Name}
            </Pill>
          ))}

          {/* +X more */}
          {hiddenCount > 0 && (
            <Pill
              color="gray"
              style={{ cursor: 'default' }}
            >
              +{hiddenCount} more
            </Pill>
          )}

          {/* Input cursor */}
          <PillsInput.Field
            placeholder={value.length === 0 ? placeholder : undefined}
            readOnly
          />
        </PillsInput>
      </Combobox.Target>

      <Combobox.Dropdown>
        <Combobox.Options style={{ maxHeight: 200, overflowY: 'auto' }}>
          {tags.map((tag) => {
            const selected = value.some((t) => t.Id === tag.Id);

            return (
              <Combobox.Option
                key={tag.Id}
                value={tag.Name}
                active={selected}
                onClick={() => toggleTag(tag)}
              >
                <div className="flex items-center gap-2 w-full">
                  {/* Color dot */}
                  <span
                    className="w-3 h-3 rounded-full"
                    style={{ backgroundColor: tag.Color }}
                  />

                  {/* Label */}
                  <span className="flex-1">{tag.Name}</span>

                  {/* Checkmark */}
                  {selected && <CheckIcon size={14} />}
                </div>
              </Combobox.Option>
            );
          })}
        </Combobox.Options>
      </Combobox.Dropdown>
    </Combobox>
  );
}