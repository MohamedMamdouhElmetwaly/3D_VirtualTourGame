package com.mobileapp.filepicker;

import android.app.Activity;
import android.content.ContentResolver;
import android.net.Uri;

import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;

/** Streams a document-provider URI to Unity's private cache without JNI byte-array marshalling. */
public final class GlbFileHelper {
    private static final int COPY_BUFFER_SIZE = 1024 * 1024;

    private GlbFileHelper() {
    }

    public static long copyUriToFile(
            Activity activity,
            String uriString,
            String destinationPath) throws IOException {
        if (activity == null) {
            throw new IOException("Android activity is unavailable");
        }

        ContentResolver resolver = activity.getContentResolver();
        Uri uri = Uri.parse(uriString);
        InputStream input = null;
        FileOutputStream output = null;

        try {
            input = resolver.openInputStream(uri);
            if (input == null) {
                throw new IOException("The selected document could not be opened");
            }

            output = new FileOutputStream(destinationPath, false);
            byte[] buffer = new byte[COPY_BUFFER_SIZE];
            long totalBytes = 0;
            int bytesRead;

            while ((bytesRead = input.read(buffer)) != -1) {
                if (bytesRead == 0) {
                    continue;
                }

                output.write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }

            output.flush();
            return totalBytes;
        } finally {
            if (output != null) {
                try {
                    output.close();
                } catch (IOException ignored) {
                }
            }

            if (input != null) {
                try {
                    input.close();
                } catch (IOException ignored) {
                }
            }
        }
    }
}
