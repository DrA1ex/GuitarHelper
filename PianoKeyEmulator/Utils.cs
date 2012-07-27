using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace PianoKeyEmulator
{
    static class Utils
    {

        public static T ConvertToEnum<T>( this string enumString )
        {
            try
            {
                return (T)Enum.Parse( typeof( T ), enumString, true );
            }
            catch( Exception ex )
            {
                // Create an instance of T ... we're doing this to that we can peform a GetType() on it to retrieve the name
                //
                T temp = default( T );
                String s = String.Format( "'{0}' is not a valid enumeration of '{1}'", enumString, temp.GetType().Name );
                throw new Exception( s, ex );
            }
        }

        static public bool CompareArrays( int[] arr0, int[] arr1 )
        {
            if( arr0.Length != arr1.Length ) return false;
            for( int i = 0; i < arr0.Length; i++ )
                if( arr0[i] != arr1[i] ) return false;
            return true;
        }

        static public int CountOfTones( Tones tone, List<Note> notes )
        {
            int count = 0;

            foreach( var current in notes )
            {
                if( current.tone == tone )
                {
                    ++count;
                }
            }

            return count;
        }

        static public void Swap<T>( List<T> lst, int x, int y )
        {
            T tmp = lst[x];
            lst[x] = lst[y];
            lst[y] = tmp;
        }

        static public IEnumerable<List<T>> GeneratePermutation<T>( List<T> list, int k = 0 )
        {
            int i;
            if( k == list.Count )
            {
                yield return list;
            }
            else
                for( i = k; i < list.Count; i++ )
                {
                    Swap( list, k, i );
                    foreach( var result in GeneratePermutation( list, k + 1 ) )
                    {
                        yield return result;
                    }
                    Swap( list, k, i );
                }

            yield break;
        }
    }

    struct ColorItem
    {
        public ColorItem( bool free, Color color )
        {
            this.free = free;
            this.color = color;
        }
        public bool free;
        public Color color;
    }
}
