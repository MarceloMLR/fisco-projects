import DescriptionOutlined from '@mui/icons-material/DescriptionOutlined';
import ImageOutlined from '@mui/icons-material/ImageOutlined';
import PictureAsPdfOutlined from '@mui/icons-material/PictureAsPdfOutlined';
import { fileKind } from './documents';

/** The icon of a file by its type, as in the canvas: PDF, image or a document. */
export function FileIcon({ name, size }: { name: string; size: number }) {
  const sx = { flex: 'none', fontSize: size, color: 'var(--text-secondary)' } as const;
  switch (fileKind(name)) {
    case 'pdf':
      return <PictureAsPdfOutlined aria-hidden sx={sx} />;
    case 'image':
      return <ImageOutlined aria-hidden sx={sx} />;
    default:
      return <DescriptionOutlined aria-hidden sx={sx} />;
  }
}
