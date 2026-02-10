import { useRef, useState } from 'react';
import { Autocomplete, Loader } from '@mantine/core';
import { sendPhotinoRequest } from '../../utils/photino';

interface SearchAutocompleteProps {
  onChange: (value: string) => void;
}

export function SearchAutocomplete({ onChange }: SearchAutocompleteProps) {
  const timeoutRef = useRef<number>(-1);
  const [value, setValue] = useState('');
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<string[]>([]);

  // debounce search for typing
  const handleInputChange = (val: string) => {
    if (val === undefined) return; // safety check
    window.clearTimeout(timeoutRef.current);
    setValue(val);

    timeoutRef.current = window.setTimeout(async () => {
      if (!val) return setData([]);
      setLoading(true);
      const response = await sendPhotinoRequest('jobSearch.getTitles', { keyword: val });
      console.log(response);
      setData(response.JobTitles || []);
      setLoading(false);
      onChange(val); // send back current input value while typing
    }, 2000);
  };

  // handle Enter key
  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      onChange(value); // send value on Enter
    }
  };

  return (
    <div className="w-full max-w-xs">
      <Autocomplete
        value={value}
        data={data}
        onChange={(val) => {
          if (val === undefined) return; // safety check
          setValue(val);
          handleInputChange(val); // always treat as input change
        }}
        onKeyDown={handleKeyDown}
        placeholder="Search job titles..."
        rightSection={loading ? <Loader size={16} /> : null}
      />
    </div>
  );
}
